using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.SqlClient;
using Scalar.AspNetCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Logica;
using FutbolYaAPI.Repositorios;
using FutbolYaAPI.Endpoints;
using FutbolYaAPI.Seguridad;
using FutbolYaAPI.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión vive en el appsettings.json base (autenticación de Windows, sin
// contraseña), pero si por algún motivo falta (p. ej. se borró a mano, o un entorno la vacía
// sin reemplazarla) la API falla al arrancar con un mensaje claro, en vez de que EF Core lance
// una excepción críptica en el primer request.
var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection'. Configurala en appsettings.json " +
        "(o, si el entorno la sobrescribe, en user-secrets/variable de entorno 'ConnectionStrings__DefaultConnection').");
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(cadenaConexion));

// ── Repositorios ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IVentaDetalladaRepository, VentaDetalladaRepository>();
builder.Services.AddScoped<ICanchaRepository, CanchaRepository>();
builder.Services.AddScoped<IHorarioDisponibleRepository, HorarioDisponibleRepository>();
builder.Services.AddScoped<ICanchaHorarioRepository, CanchaHorarioRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaHorarioRepository, ReservaHorarioRepository>();
builder.Services.AddScoped<IReservaMaterialRepository, ReservaMaterialRepository>();
builder.Services.AddScoped<IMaterialDeportivoRepository, MaterialDeportivoRepository>();
builder.Services.AddScoped<IEstadisticaRepository, EstadisticaRepository>();
builder.Services.AddScoped<IEstadisticaCanchaRepository, EstadisticaCanchaRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

// Abstracción de transacciones para que Logica pueda envolver varias escrituras
// (Venta/Reserva + descuentos de stock) sin depender de EF directamente.
builder.Services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

// ── Lógica de negocio ────────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioLogica, UsuarioLogica>();
builder.Services.AddScoped<IProductoLogica, ProductoLogica>();
builder.Services.AddScoped<IVentaLogica, VentaLogica>();
builder.Services.AddScoped<ICanchaLogica, CanchaLogica>();
builder.Services.AddScoped<IHorarioDisponibleLogica, HorarioDisponibleLogica>();
builder.Services.AddScoped<IReservaLogica, ReservaLogica>();
builder.Services.AddScoped<IMaterialDeportivoLogica, MaterialDeportivoLogica>();
builder.Services.AddScoped<IEstadisticaLogica, EstadisticaLogica>();
builder.Services.AddScoped<IEstadisticaCanchaLogica, EstadisticaCanchaLogica>();
builder.Services.AddScoped<IAuditoriaLogica, AuditoriaLogica>();

// ── Seguridad (JWT + hashing + envío de correo) ────────────────────────────
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' en appsettings.json");

// La clave que firma los JWT nunca se versiona. En desarrollo, si no hay una configurada, se
// genera una aleatoria al arrancar para que el proyecto levante sin configuración previa (los
// tokens emitidos dejan de valer al reiniciar la API y hay que volver a iniciar sesión). En
// cualquier otro entorno es obligatoria: sin ella la API no arranca, para no quedar expuesta
// con una clave predecible.
const string PlaceholderSecretoViejo = "CAMBIAR-ESTA-CLAVE-EN-PRODUCCION-por-una-de-al-menos-32-caracteres";
var secretoJwtGenerado = false;
if (string.IsNullOrWhiteSpace(jwtOptions.Secreto) ||
    jwtOptions.Secreto.Length < 32 ||
    jwtOptions.Secreto == PlaceholderSecretoViejo)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Falta configurar 'Jwt:Secreto' (al menos 32 caracteres). Configurarlo con la variable " +
            "de entorno 'Jwt__Secreto' o en la configuración de la aplicación.");
    }

    jwtOptions.Secreto = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    secretoJwtGenerado = true;
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtOptions.Emisor,
            ValidAudience            = jwtOptions.Audiencia,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secreto)),
            ClockSkew                = TimeSpan.FromSeconds(30)
        };

        // El JWT dura 8 horas (MinutosExpiracion); validar solo firma/expiración no alcanza,
        // porque un usuario podría ser borrado/desactivado o cambiar de rol mientras el token
        // sigue vigente. Por eso cada request autenticado se revalida contra la base: existe,
        // está Activo, y el rol del claim coincide con el rol actual. Una sola consulta
        // AsNoTracking trayendo solo lo necesario (Activo, Rol), para no pagar el costo de un
        // JOIN/entidad completa en cada request.
        opt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var codUsuarioClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(codUsuarioClaim, out var codUsuario))
                {
                    context.Fail("Token inválido");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var usuario = await db.Usuarios.AsNoTracking()
                    .Where(u => u.Cod_Usuario == codUsuario)
                    .Select(u => new { u.Activo, u.Rol })
                    .FirstOrDefaultAsync();

                if (usuario == null || !usuario.Activo)
                {
                    context.Fail("El usuario ya no existe o está inactivo");
                    return;
                }

                var rolClaim = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var rolActual = usuario.Rol ? "Administrador" : "Operador";
                if (rolClaim != rolActual)
                {
                    context.Fail("El rol del token ya no coincide con el del usuario");
                }
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SoloAdministrador", p => p.RequireRole("Administrador"));

// Unifica el formato JSON de la API a snake_case, tal como lo describe la documentación:
// el conversor por defecto de .NET solo baja la primera letra de cada nombre compuesto,
// dejando cosas como "cod_Usuario".
builder.Services.ConfigureHttpJsonOptions(opt =>
{
    opt.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    // Los enums (EstadoCancha, etc.) viajan como texto ("Disponible"), no como número.
    opt.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

// Rate limiting con el paquete integrado de ASP.NET Core (sin dependencias nuevas),
// particionado por IP del cliente. La MVC llama a la API server-to-server,
// así que RemoteIpAddress de por sí solo vería la IP de la MVC para todos los usuarios del
// navegador. Para no perder esa granularidad, la MVC (AuthService) manda la IP real del
// navegador en el header "X-Cliente-IP" — pero ese header es un dato que cualquier cliente
// externo podría falsificar para evadir su propio límite, así que la API solo lo usa cuando
// puede confiar en que vino de la MVC y no de un llamante externo: conexión física loopback
// (caso de desarrollo, MVC y API en el mismo host) o header "X-Clave-Interna" con el valor
// configurado en "ClaveInterna" (caso de Azure, donde MVC y API son dos App Service distintas
// y ya no comparten host). La comparación es en tiempo constante para no filtrar la clave por
// timing. Sin "ClaveInterna" configurada solo aplica la regla de loopback. Cualquier otra IP de
// origen sin clave válida usa RemoteIpAddress directo, e IP de origen loopback sin header válido
// cae al mismo RemoteIpAddress (loopback), sin romper el límite existente.
var claveInterna = builder.Configuration["ClaveInterna"];
var claveInternaBytes = string.IsNullOrEmpty(claveInterna) ? null : Encoding.UTF8.GetBytes(claveInterna);

builder.Services.AddRateLimiter(opt =>
{
    opt.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { mensaje = "Demasiados intentos. Esperá unos minutos e intentá de nuevo." },
            cancellationToken: token);
    };

    string ClavePorIp(HttpContext http)
    {
        var remota = http.Connection.RemoteIpAddress;
        var esLoopback = remota != null && IPAddress.IsLoopback(remota);

        var claveInternaValida = claveInternaBytes != null
            && http.Request.Headers.TryGetValue("X-Clave-Interna", out var claveHeader)
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(claveHeader.ToString()), claveInternaBytes);

        if ((esLoopback || claveInternaValida)
            && http.Request.Headers.TryGetValue("X-Cliente-IP", out var valorHeader)
            && IPAddress.TryParse(valorHeader.ToString(), out var ipCliente))
        {
            return ipCliente.ToString();
        }

        return remota?.ToString() ?? "desconocida";
    }

    opt.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        ClavePorIp(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    opt.AddPolicy("solicitar-recuperacion", http => RateLimitPartition.GetFixedWindowLimiter(
        ClavePorIp(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));

    opt.AddPolicy("confirmar-recuperacion", http => RateLimitPartition.GetFixedWindowLimiter(
        ClavePorIp(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
});

// Crea el Administrador inicial (config "AdminInicial") en el primer arranque, si la tabla de
// usuarios está vacía. Va registrado ANTES de
// BarridoReservasService para correr primero (ambos IHostedService corren en orden de registro).
builder.Services.AddHostedService<SembradorAdminInicial>();

// Corre ConfirmarVencidas en este BackgroundService cada N minutos (config
// "Reservas:MinutosBarrido", default 5) y una vez al arrancar, en vez de dentro de cada
// GET /api/reservas[/{id}], para que un GET no tenga efectos secundarios de escritura.
builder.Services.AddHostedService<BarridoReservasService>();

// App Service termina la conexión TLS y reenvía el request al proceso de la app por HTTP,
// agregando su propia entrada X-Forwarded-For/-Proto con la IP/esquema real del cliente.
// ForwardLimit en su valor por defecto (1) hace que solo se confíe en esa única entrada
// agregada por el front-end, y no en las que un cliente externo pudiera agregar de más para
// falsificar su propia IP.
builder.Services.Configure<ForwardedHeadersOptions>(opt =>
{
    opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opt.KnownIPNetworks.Clear();
    opt.KnownProxies.Clear();
});

var app = builder.Build();

if (secretoJwtGenerado)
{
    app.Logger.LogWarning(
        "No hay 'Jwt:Secreto' configurado: se generó una clave aleatoria para esta ejecución (solo en Development).");
}

// Aplica las migraciones pendientes antes de aceptar requests y antes de que arranquen los
// servicios en segundo plano (el sembrador del admin y el barrido de reservas necesitan las
// tablas). Crea la base si no existe, así una instalación nueva no requiere pasos manuales.
// La base serverless de Azure SQL se pausa tras un período de inactividad; el primer intento de
// conexión mientras está pausada/reanudando falla con un error transitorio en vez de conectar
// directo, así que se reintenta unas veces antes de darlo por perdido.
const int intentosMaximosMigracion = 6;
var erroresTransitoriosMigracion = new[] { 40613, 40197, 40501, 49918, 49919, 49920, 4060, 233, -2, 10928, 10929, 10053, 10054, 10060 };

static bool EsErrorTransitorioDeConexion(Exception ex, int[] erroresTransitorios)
{
    for (var actual = ex; actual != null; actual = actual.InnerException)
    {
        if (actual is SqlException sql && Array.IndexOf(erroresTransitorios, sql.Number) >= 0)
            return true;
    }
    return false;
}

for (var intentoMigracion = 1; intentoMigracion <= intentosMaximosMigracion; intentoMigracion++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        break;
    }
    catch (Exception ex) when (intentoMigracion < intentosMaximosMigracion && EsErrorTransitorioDeConexion(ex, erroresTransitoriosMigracion))
    {
        app.Logger.LogWarning(ex,
            "Fallo transitorio aplicando migraciones al arrancar (intento {Intento} de {Max}); la base puede estar " +
            "pausada/reanudando. Reintentando en 10 s.",
            intentoMigracion, intentosMaximosMigracion);
        Thread.Sleep(TimeSpan.FromSeconds(10));
    }
}

// Configure the HTTP request pipeline.
// Primero de todo el pipeline: hasta que corre, HttpContext.Connection.RemoteIpAddress y
// Request.Scheme siguen mostrando el proxy de App Service en vez del cliente/esquema real.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapAuthEndpoints();
app.MapUsuarioEndpoints();
app.MapProductoEndpoints();
app.MapVentaEndpoints();
app.MapCanchaEndpoints();
app.MapHorarioDisponibleEndpoints();
app.MapReservaEndpoints();
app.MapMaterialDeportivoEndpoints();
app.MapEstadisticaEndpoints();
app.MapEstadisticaCanchaEndpoints();
app.MapAuditoriaEndpoints();

app.Run();
