using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IUsuarioLogica
{
    Task<IEnumerable<UsuarioDto>> ObtenerTodos();
    Task<UsuarioDto?> ObtenerPorId(int id);
    Task<(UsuarioDto? resultado, string? error)> Crear(UsuarioCreateDto dto);
    Task<(UsuarioDto? resultado, string? error)> Actualizar(int id, UsuarioUpdateDto dto);
    Task<(CambiarContraseñaResponseDto? resultado, string? error)> CambiarContrasena(int id, CambiarContraseñaDto dto);
    Task<(LoginResponseDto? resultado, string? error)> Login(LoginDto dto);
    Task<(bool eliminado, string? error)> Eliminar(int id);
}

public class UsuarioLogica : IUsuarioLogica
{
    private readonly IUsuarioRepository _repo;

    public UsuarioLogica(IUsuarioRepository repo)
    {
        _repo = repo;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static UsuarioDto MapDto(Usuario u) =>
        new UsuarioDto(
            u.Cod_Usuario,
            u.Nombre,
            u.Apellido,
            u.Dni,
            u.Direccion,
            u.Correo,
            u.Rol,
            u.Cambiar_Contraseña
        );

    public async Task<IEnumerable<UsuarioDto>> ObtenerTodos()
    {
        var usuarios = await _repo.ObtenerTodos();
        return usuarios.Select(MapDto);
    }

    public async Task<UsuarioDto?> ObtenerPorId(int id)
    {
        var u = await _repo.ObtenerPorId(id);
        if (u == null) return null;
        return MapDto(u);
    }

    public async Task<(UsuarioDto? resultado, string? error)> Crear(UsuarioCreateDto dto)
    {
        // Verificar DNI y correo únicos
        var usuarios = await _repo.ObtenerTodos();

        if (usuarios.Any(u => u.Dni == dto.Dni))
            return (null, "Ya existe un usuario con ese DNI");

        if (usuarios.Any(u => u.Correo == dto.Correo))
            return (null, "Ya existe un usuario con ese correo");

        var usuario = new Usuario
        {
            Nombre             = dto.Nombre,
            Apellido           = dto.Apellido,
            Dni                = dto.Dni,
            Direccion          = dto.Direccion,
            Correo             = dto.Correo,
            Contraseña         = dto.Contraseña,
            Rol                = dto.Rol,
            Cambiar_Contraseña = true   // siempre true al crear
        };

        await _repo.Agregar(usuario);
        return (MapDto(usuario), null);
    }

    public async Task<(UsuarioDto? resultado, string? error)> Actualizar(int id, UsuarioUpdateDto dto)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (null, "NOT_FOUND");

        // Verificar que DNI y correo no los use otro usuario
        var usuarios = await _repo.ObtenerTodos();

        if (usuarios.Any(u => u.Dni == dto.Dni && u.Cod_Usuario != id))
            return (null, "Ya existe un usuario con ese DNI");

        if (usuarios.Any(u => u.Correo == dto.Correo && u.Cod_Usuario != id))
            return (null, "Ya existe un usuario con ese correo");

        usuario.Nombre     = dto.Nombre;
        usuario.Apellido   = dto.Apellido;
        usuario.Dni        = dto.Dni;
        usuario.Direccion  = dto.Direccion;
        usuario.Correo     = dto.Correo;
        usuario.Rol        = dto.Rol;

        await _repo.Actualizar(usuario);
        return (MapDto(usuario), null);
    }

    public async Task<(CambiarContraseñaResponseDto? resultado, string? error)> CambiarContrasena(int id, CambiarContraseñaDto dto)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (null, "NOT_FOUND");

        if (usuario.Contraseña != dto.Contrasena_Actual)
            return (null, "La contraseña actual es incorrecta");

        usuario.Contraseña         = dto.Contrasena_Nueva;
        usuario.Cambiar_Contraseña = false;  // ya cambió su contraseña

        await _repo.Actualizar(usuario);

        return (new CambiarContraseñaResponseDto(
            "Contraseña actualizada correctamente",
            usuario.Cambiar_Contraseña
        ), null);
    }

    public async Task<(LoginResponseDto? resultado, string? error)> Login(LoginDto dto)
    {
        var usuarios = await _repo.ObtenerTodos();
        var usuario  = usuarios.FirstOrDefault(u =>
            u.Correo     == dto.Correo &&
            u.Contraseña == dto.Contrasena);

        if (usuario == null)
            return (null, "Credenciales incorrectas");

        return (new LoginResponseDto(
            usuario.Cod_Usuario,
            usuario.Nombre,
            usuario.Rol,
            usuario.Cambiar_Contraseña
        ), null);
    }

    public async Task<(bool eliminado, string? error)> Eliminar(int id)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null)
            return (false, "NOT_FOUND");

        // No se puede eliminar un Administrador
        if (usuario.Rol == true)
            return (false, "No se puede eliminar un usuario con rol Administrador");

        await _repo.Eliminar(usuario);
        return (true, null);
    }
}