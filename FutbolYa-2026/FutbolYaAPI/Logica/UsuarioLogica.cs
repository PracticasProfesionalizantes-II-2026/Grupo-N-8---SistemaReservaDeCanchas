using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Repositorios;
using FutbolYaAPI.Seguridad;

namespace FutbolYaAPI.Logica;

public interface IUsuarioLogica
{
    Task<IEnumerable<UsuarioDto>> ObtenerTodos(bool incluirBajas = false);
    Task<UsuarioDto?> ObtenerPorId(int id);
    Task<(UsuarioDto? resultado, string? error, int? codUsuarioInactivo)> Crear(UsuarioCreateDto dto, int codUsuarioAccion);
    Task<(UsuarioDto? resultado, string? error)> Reactivar(int id, UsuarioCreateDto dto, int codUsuarioAccion);
    Task<(UsuarioDto? resultado, string? error)> Actualizar(int id, UsuarioUpdateDto dto, int codUsuarioAccion);
    Task<(MensajeResponseDto? resultado, string? error)> CambiarContrasena(int id, CambiarContraseñaDto dto);
    Task<(MensajeResponseDto resultado, string? error)> SolicitarRecuperacion(SolicitarRecuperacionDto dto);
    Task<(MensajeResponseDto? resultado, string? error)> IniciarReseteoPorAdmin(int id, int codUsuarioAccion);
    Task<(MensajeResponseDto? resultado, string? error)> ConfirmarRecuperacion(ConfirmarRecuperacionDto dto);
    Task<(LoginResponseDto? resultado, string? error)> Login(LoginDto dto);
    Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion);
}

// Lógica de negocio de usuarios: valida datos, controla duplicados de DNI/correo,
// gestiona el ciclo de alta/baja/reactivación y el flujo de recuperación de contraseña.
// Se ubica entre los Endpoints y el UsuarioRepository.
public class UsuarioLogica : IUsuarioLogica
{
    private const int MinutosValidezCodigo = 15;

    private static readonly Regex RegexCorreo = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex RegexDni = new(@"^\d{7,8}$", RegexOptions.Compiled);

    private readonly IUsuarioRepository _repo;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly IAuditoriaLogica _auditoria;
    private readonly ILogger<UsuarioLogica> _logger;

    public UsuarioLogica(
        IUsuarioRepository repo,
        IJwtService jwt,
        IEmailService email,
        IAuditoriaLogica auditoria,
        ILogger<UsuarioLogica> logger)
    {
        _repo      = repo;
        _jwt       = jwt;
        _email     = email;
        _auditoria = auditoria;
        _logger    = logger;
    }

    private static UsuarioDto MapDto(Usuario u) => new(
        u.Cod_Usuario, u.Nombre, u.Apellido, u.Dni, u.Direccion, u.Correo, u.Rol, u.Cambiar_Contraseña, u.Activo
    );

    private static string? ValidarDatosBasicos(string nombre, string apellido, string dni, string correo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(correo))
            return "Todos los campos son obligatorios";

        if (!RegexDni.IsMatch(dni))
            return "El DNI debe tener entre 7 y 8 dígitos numéricos";

        if (!RegexCorreo.IsMatch(correo))
            return "El formato del correo no es válido";

        return null;
    }

    private static string? ValidarContraseña(string contraseña)
    {
        if (contraseña.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres";
        return null;
    }

    // Usa RandomNumberGenerator (no Random.Shared) porque el código se usa como credencial
    // temporal de recuperación de contraseña y necesita ser criptográficamente impredecible.
    private static string GenerarCodigo() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public async Task<IEnumerable<UsuarioDto>> ObtenerTodos(bool incluirBajas = false)
    {
        var usuarios = await _repo.ObtenerTodos();
        if (!incluirBajas)
            usuarios = usuarios.Where(u => u.Activo);

        return usuarios.Select(MapDto);
    }

    public async Task<UsuarioDto?> ObtenerPorId(int id)
    {
        var u = await _repo.ObtenerPorId(id);
        return u == null ? null : MapDto(u);
    }

    // Alta de usuario: valida datos y contraseña, controla duplicados de DNI/correo contra
    // TODOS los usuarios (activos e inactivos) y crea el registro con contraseña hasheada.
    public async Task<(UsuarioDto? resultado, string? error, int? codUsuarioInactivo)> Crear(UsuarioCreateDto dto, int codUsuarioAccion)
    {
        // Se recorta espacios antes de comparar/guardar: sin trim, "ricardo " y "ricardo"
        // se tratarían como valores distintos en el chequeo de duplicados.
        var nombre = dto.Nombre.Trim();
        var dni    = dto.Dni.Trim();
        var correo = dto.Correo.Trim();

        var errorBasico = ValidarDatosBasicos(nombre, dto.Apellido, dni, correo);
        if (errorBasico != null)
            return (null, errorBasico, null);

        var errorContraseña = ValidarContraseña(dto.Contraseña);
        if (errorContraseña != null)
            return (null, errorContraseña, null);

        // El chequeo de duplicados se hace contra todos los usuarios, no solo los Activos: no hay
        // índice único en Usuario.Dni/Correo, así que esta comparación es la única barrera contra
        // dos filas con el mismo DNI/correo (incluidas las dadas de baja).
        var usuarios = await _repo.ObtenerTodos();

        var duplicadoDni = usuarios.FirstOrDefault(u => u.Dni == dni);
        if (duplicadoDni != null)
        {
            if (duplicadoDni.Activo)
                return (null, "Ya existe un usuario activo con ese DNI", null);
            return (null, "Ya existe un usuario dado de baja con ese DNI. ¿Querés reactivarlo en vez de crear uno nuevo?", duplicadoDni.Cod_Usuario);
        }

        var duplicadoCorreo = usuarios.FirstOrDefault(u => u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));
        if (duplicadoCorreo != null)
        {
            if (duplicadoCorreo.Activo)
                return (null, "Ya existe un usuario activo con ese correo", null);
            return (null, "Ya existe un usuario dado de baja con ese correo. ¿Querés reactivarlo en vez de crear uno nuevo?", duplicadoCorreo.Cod_Usuario);
        }

        var usuario = new Usuario
        {
            Nombre             = nombre,
            Apellido           = dto.Apellido,
            Dni                = dni,
            Direccion          = dto.Direccion,
            Correo             = correo,
            Contraseña         = PasswordHasher.Hash(dto.Contraseña),
            Rol                = dto.Rol,
            Cambiar_Contraseña = true,   // todo usuario nuevo debe cambiar su contraseña temporal al ingresar
            Activo             = true
        };

        await _repo.Agregar(usuario);
        var resultado = MapDto(usuario);
        await _auditoria.Registrar(codUsuarioAccion, "Usuario", usuario.Cod_Usuario, AccionAuditoria.Alta, null, resultado);
        return (resultado, null, null);
    }

    // Reactiva un usuario dado de baja, reemplazando sus datos por los nuevos que se
    // intentaron cargar (el admin ya escribió nombre/apellido/rol/contraseña en el alta original).
    public async Task<(UsuarioDto? resultado, string? error)> Reactivar(int id, UsuarioCreateDto dto, int codUsuarioAccion)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (null, "NOT_FOUND");
        if (usuario.Activo)
            return (null, "El usuario ya está activo");

        var nombre = dto.Nombre.Trim();
        var dni    = dto.Dni.Trim();
        var correo = dto.Correo.Trim();

        var errorBasico = ValidarDatosBasicos(nombre, dto.Apellido, dni, correo);
        if (errorBasico != null)
            return (null, errorBasico);

        var errorContraseña = ValidarContraseña(dto.Contraseña);
        if (errorContraseña != null)
            return (null, errorContraseña);

        var usuarios = await _repo.ObtenerTodos();
        if (usuarios.Any(u => u.Activo && u.Dni == dni && u.Cod_Usuario != id))
            return (null, "Ya existe otro usuario activo con ese DNI");
        if (usuarios.Any(u => u.Activo && u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase) && u.Cod_Usuario != id))
            return (null, "Ya existe otro usuario activo con ese correo");

        var anterior = MapDto(usuario);
        usuario.Nombre             = nombre;
        usuario.Apellido           = dto.Apellido;
        usuario.Dni                = dni;
        usuario.Direccion          = dto.Direccion;
        usuario.Correo             = correo;
        usuario.Contraseña         = PasswordHasher.Hash(dto.Contraseña);
        usuario.Rol                = dto.Rol;
        usuario.Cambiar_Contraseña = true;
        usuario.Activo             = true;

        await _repo.Actualizar(usuario);
        var resultado = MapDto(usuario);
        await _auditoria.Registrar(codUsuarioAccion, "Usuario", id, AccionAuditoria.Alta, anterior, resultado);
        return (resultado, null);
    }

    public async Task<(UsuarioDto? resultado, string? error)> Actualizar(int id, UsuarioUpdateDto dto, int codUsuarioAccion)
    {
        var nombre = dto.Nombre.Trim();
        var dni    = dto.Dni.Trim();
        var correo = dto.Correo.Trim();

        var errorBasico = ValidarDatosBasicos(nombre, dto.Apellido, dni, correo);
        if (errorBasico != null)
            return (null, errorBasico);

        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (null, "NOT_FOUND");

        var usuarios = await _repo.ObtenerTodos();
        if (usuarios.Any(u => u.Activo && u.Dni == dni && u.Cod_Usuario != id))
            return (null, "Ya existe un usuario con ese DNI");
        if (usuarios.Any(u => u.Activo && u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase) && u.Cod_Usuario != id))
            return (null, "Ya existe un usuario con ese correo");

        // No puede quedar el sistema sin ningún Administrador activo: se perdería el acceso de gestión.
        if (usuario.Rol && !dto.Rol)
        {
            var adminsActivos = usuarios.Count(u => u.Activo && u.Rol);
            if (adminsActivos <= 1)
                return (null, "No se puede quitar el rol de Administrador: es el único administrador activo del sistema");
        }

        var anterior = MapDto(usuario);
        usuario.Nombre    = nombre;
        usuario.Apellido  = dto.Apellido;
        usuario.Dni       = dni;
        usuario.Direccion = dto.Direccion;
        usuario.Correo    = correo;
        usuario.Rol       = dto.Rol;

        await _repo.Actualizar(usuario);
        var resultado = MapDto(usuario);
        await _auditoria.Registrar(codUsuarioAccion, "Usuario", id, AccionAuditoria.Modificacion, anterior, resultado);
        return (resultado, null);
    }

    public async Task<(MensajeResponseDto? resultado, string? error)> CambiarContrasena(int id, CambiarContraseñaDto dto)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (null, "NOT_FOUND");

        if (!PasswordHasher.Verificar(dto.Contrasena_Actual, usuario.Contraseña))
            return (null, "La contraseña actual es incorrecta");

        var errorContraseña = ValidarContraseña(dto.Contrasena_Nueva);
        if (errorContraseña != null)
            return (null, errorContraseña);

        if (dto.Contrasena_Nueva != dto.Contrasena_Nueva_Confirmacion)
            return (null, "La nueva contraseña y su confirmación no coinciden");

        usuario.Contraseña         = PasswordHasher.Hash(dto.Contrasena_Nueva);
        usuario.Cambiar_Contraseña = false;
        await _repo.Actualizar(usuario);

        return (new MensajeResponseDto("Contraseña actualizada correctamente"), null);
    }

    // Flujo "¿Olvidé mi contraseña?" desde el login: genera y envía un código de un solo uso.
    // Siempre responde el mismo mensaje genérico exista o no el correo, para no revelar
    // por enumeración qué direcciones están registradas en el sistema.
    public async Task<(MensajeResponseDto resultado, string? error)> SolicitarRecuperacion(SolicitarRecuperacionDto dto)
    {
        var usuarios = await _repo.ObtenerTodos();
        var usuario = usuarios.FirstOrDefault(u => u.Activo && u.Correo.Equals(dto.Correo, StringComparison.OrdinalIgnoreCase));

        if (usuario != null)
        {
            var codigo = GenerarCodigo();
            usuario.Token_Recuperacion = codigo;
            usuario.Token_Expira       = DateTime.Now.AddMinutes(MinutosValidezCodigo);
            await _repo.Actualizar(usuario);

            // Si el envío falla (SMTP caído, credenciales revocadas) el error no se propaga: se
            // loguea y se responde igual el mismo mensaje genérico, para no revelar por el código
            // de estado qué correos están registrados. El usuario puede volver a pedirlo.
            try
            {
                await _email.EnviarAsync(usuario.Correo, "Recuperación de contraseña - Fútbol Ya",
                    $"Tu código de verificación es: {codigo}\nEs válido por {MinutosValidezCodigo} minutos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de recuperación al usuario {CodUsuario}", usuario.Cod_Usuario);
            }
        }

        return (new MensajeResponseDto("Si el correo está registrado, vas a recibir un código de verificación."), null);
    }

    // El administrador dispara el restablecimiento de contraseña de un usuario: se envía un código
    // de verificación por correo en vez de asignar una contraseña temporal en texto plano.
    public async Task<(MensajeResponseDto? resultado, string? error)> IniciarReseteoPorAdmin(int id, int codUsuarioAccion)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (null, "NOT_FOUND");

        // Se guarda el estado anterior para poder revertirlo si el envío del correo falla, así el
        // usuario no queda obligado a cambiar la contraseña con un código que nunca le llegó.
        var tokenAnterior    = usuario.Token_Recuperacion;
        var expiraAnterior   = usuario.Token_Expira;
        var cambiarAnterior  = usuario.Cambiar_Contraseña;

        var codigo = GenerarCodigo();
        usuario.Token_Recuperacion = codigo;
        usuario.Token_Expira       = DateTime.Now.AddMinutes(MinutosValidezCodigo);
        usuario.Cambiar_Contraseña = true;
        await _repo.Actualizar(usuario);

        try
        {
            await _email.EnviarAsync(usuario.Correo, "Restablecimiento de contraseña - Fútbol Ya",
                $"Un administrador solicitó el restablecimiento de tu contraseña.\nTu código de verificación es: {codigo}\nEs válido por {MinutosValidezCodigo} minutos.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo de restablecimiento al usuario {CodUsuario}", usuario.Cod_Usuario);

            usuario.Token_Recuperacion = tokenAnterior;
            usuario.Token_Expira       = expiraAnterior;
            usuario.Cambiar_Contraseña = cambiarAnterior;
            await _repo.Actualizar(usuario);

            return (null, $"No se pudo enviar el correo a {usuario.Correo}. Revisá la configuración de correo e intentá nuevamente; no se modificó nada del usuario.");
        }

        await _auditoria.Registrar(codUsuarioAccion, "Usuario", id, AccionAuditoria.Modificacion, null, new { Accion = "Reseteo de contraseña solicitado" });
        return (new MensajeResponseDto($"Se envió un código a {usuario.Correo}."), null);
    }

    // Verifica el código de recuperación (correo + código + vigencia) y, si es válido,
    // establece la nueva contraseña, cerrando el flujo iniciado en SolicitarRecuperacion.
    public async Task<(MensajeResponseDto? resultado, string? error)> ConfirmarRecuperacion(ConfirmarRecuperacionDto dto)
    {
        var usuarios = await _repo.ObtenerTodos();
        var usuario = usuarios.FirstOrDefault(u => u.Activo && u.Correo.Equals(dto.Correo, StringComparison.OrdinalIgnoreCase));

        if (usuario == null || usuario.Token_Recuperacion == null || usuario.Token_Expira == null)
            return (null, "Código inválido o vencido");

        if (usuario.Token_Recuperacion != dto.Codigo)
            return (null, "Código inválido o vencido");

        if (usuario.Token_Expira < DateTime.Now)
            return (null, "Código inválido o vencido");

        var errorContraseña = ValidarContraseña(dto.Contrasena_Nueva);
        if (errorContraseña != null)
            return (null, errorContraseña);

        if (dto.Contrasena_Nueva != dto.Contrasena_Nueva_Confirmacion)
            return (null, "La nueva contraseña y su confirmación no coinciden");

        usuario.Contraseña         = PasswordHasher.Hash(dto.Contrasena_Nueva);
        usuario.Cambiar_Contraseña = false;
        usuario.Token_Recuperacion = null;
        usuario.Token_Expira       = null;
        await _repo.Actualizar(usuario);

        return (new MensajeResponseDto("Contraseña actualizada correctamente"), null);
    }

    public async Task<(LoginResponseDto? resultado, string? error)> Login(LoginDto dto)
    {
        var usuarios = await _repo.ObtenerTodos();
        var usuario = usuarios.FirstOrDefault(u => u.Correo.Equals(dto.Correo, StringComparison.OrdinalIgnoreCase));

        if (usuario == null || !usuario.Activo || !PasswordHasher.Verificar(dto.Contrasena, usuario.Contraseña))
            return (null, "Credenciales incorrectas");

        var token = _jwt.GenerarToken(usuario);

        return (new LoginResponseDto(
            usuario.Cod_Usuario,
            usuario.Nombre,
            usuario.Rol,
            usuario.Cambiar_Contraseña,
            token
        ), null);
    }

    // Da de baja lógicamente a un usuario, bloqueando la operación si es el único Administrador activo.
    public async Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (false, "NOT_FOUND");

        if (!usuario.Activo)
            return (false, "El usuario ya está dado de baja");

        if (usuario.Rol == true)
            return (false, "No se puede dar de baja a un usuario con rol Administrador");

        // Baja lógica: se conserva el historial de reservas/ventas asociadas.
        var anterior = MapDto(usuario);
        usuario.Activo = false;
        await _repo.Actualizar(usuario);

        await _auditoria.Registrar(codUsuarioAccion, "Usuario", id, AccionAuditoria.Baja, anterior, null);
        return (true, null);
    }
}
