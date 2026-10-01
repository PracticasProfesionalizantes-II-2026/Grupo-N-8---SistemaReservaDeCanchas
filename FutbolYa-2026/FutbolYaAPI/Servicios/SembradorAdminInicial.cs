using System.Text.RegularExpressions;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace FutbolYaAPI.Servicios;

// En una instalación nueva no hay ningún usuario cargado, así que nadie podría loguearse para
// crear el primer Administrador desde la UI (huevo y gallina). Este IHostedService corre una
// sola vez al arrancar la API: si la tabla de usuarios está vacía, crea un Administrador con los
// datos de la sección de configuración "AdminInicial" (ver appsettings.json), con
// Cambiar_Contraseña = true para forzar que la cambie en el primer login. Es idempotente: con al
// menos un usuario cargado (de cualquier rol o estado) no hace nada. No usa IUnidadDeTrabajo/Auditoria a propósito: es un bootstrap del sistema, no una
// acción de un usuario autenticado (no hay Cod_Usuario "actor" válido todavía).
public class SembradorAdminInicial : IHostedService
{
    // Mismas reglas que UsuarioLogica (Dni 7-8 dígitos, correo básico, contraseña >= 8
    // caracteres). UsuarioLogica las mantiene privadas, así que se replican acá en vez de
    // depender de un detalle interno de otra clase.
    private static readonly Regex RegexCorreo = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex RegexDni = new(@"^\d{7,8}$", RegexOptions.Compiled);
    private const int LongitudMinimaContraseña = 8;

    // Solo en Development, para que el proyecto levante sin configuración previa. Es pública (está
    // en el código), por eso el admin se crea con Cambiar_Contraseña = true y fuera de Development
    // la contraseña es obligatoria desde la configuración.
    private const string ContraseñaPorDefectoDesarrollo = "Admin1234";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SembradorAdminInicial> _logger;
    private readonly IConfiguration _configuracion;
    private readonly IHostEnvironment _entorno;

    public SembradorAdminInicial(
        IServiceScopeFactory scopeFactory,
        ILogger<SembradorAdminInicial> logger,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuracion = configuracion;
        _entorno = entorno;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await db.Usuarios.AnyAsync(cancellationToken))
                return;

            var seccion = _configuracion.GetSection("AdminInicial");
            var nombre = seccion["Nombre"];
            var apellido = seccion["Apellido"];
            var dni = seccion["Dni"];
            var direccion = string.IsNullOrWhiteSpace(seccion["Direccion"]) ? "-" : seccion["Direccion"];
            var correo = seccion["Correo"];
            // "Contrasena" sin ñ para poder cargarla como variable de entorno en hosts Linux;
            // "Contraseña" se mantiene por compatibilidad con configuraciones existentes.
            var contraseña = seccion["Contrasena"] ?? seccion["Contraseña"];

            if (string.IsNullOrWhiteSpace(contraseña) && _entorno.IsDevelopment())
            {
                contraseña = ContraseñaPorDefectoDesarrollo;
                _logger.LogWarning(
                    "'AdminInicial:Contraseña' no está configurada: el Administrador inicial se crea con la " +
                    "contraseña por defecto de desarrollo y deberá cambiarla en el primer ingreso.");
            }

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contraseña))
            {
                _logger.LogWarning(
                    "No se creó el Administrador inicial: faltan datos en la sección 'AdminInicial' de la " +
                    "configuración (Nombre/Apellido/Dni/Correo/Contraseña). Fuera de Development la " +
                    "Contraseña es obligatoria: variable de entorno 'AdminInicial__Contrasena'.");
                return;
            }

            if (!RegexDni.IsMatch(dni))
            {
                _logger.LogWarning("No se creó el Administrador inicial: 'AdminInicial:Dni' debe tener entre 7 y 8 dígitos numéricos.");
                return;
            }

            if (!RegexCorreo.IsMatch(correo))
            {
                _logger.LogWarning("No se creó el Administrador inicial: 'AdminInicial:Correo' no tiene un formato válido.");
                return;
            }

            if (contraseña.Length < LongitudMinimaContraseña)
            {
                _logger.LogWarning(
                    "No se creó el Administrador inicial: 'AdminInicial:Contraseña' debe tener al menos {Longitud} caracteres.",
                    LongitudMinimaContraseña);
                return;
            }

            var admin = new Usuario
            {
                Nombre = nombre.Trim(),
                Apellido = apellido.Trim(),
                Dni = dni.Trim(),
                Direccion = direccion!.Trim(),
                Correo = correo.Trim(),
                Contraseña = PasswordHasher.Hash(contraseña),
                Rol = true,
                Activo = true,
                Cambiar_Contraseña = true
            };

            db.Usuarios.Add(admin);
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Administrador inicial creado: {Correo}", admin.Correo);
        }
        catch (Exception ex)
        {
            // No debe tirar abajo el host (p. ej. si la base todavía no tiene las migraciones
            // aplicadas al primer arranque contra una BD nueva).
            _logger.LogError(ex, "Error al intentar crear el Administrador inicial");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
