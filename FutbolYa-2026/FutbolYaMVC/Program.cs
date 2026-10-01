using FutbolYaMVC.Services;
using FutbolYaMVC.Infraestructura;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Autenticación basada en sesión (sin AddAuthentication/[Authorize]): cada acción valida
// manualmente los datos de sesión (Cod_Usuario, Rol, Jwt). Filtro global de antiforgery en
// toda escritura y HttpClients tipados hacia la API con el JWT inyectado por
// AuthorizedHttpMessageHandler. MVC y la API pueden correr en hosts distintos (dos App Service
// en Azure); en ese caso "ClaveInterna" (ver más abajo) reemplaza a loopback como forma de que
// la API confíe en el X-Cliente-IP que manda esta app.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5007";
var claveInterna = builder.Configuration["ClaveInterna"];

// Add services to the container.
builder.Services.AddControllersWithViews(opt =>
{
    opt.Filters.Add<ExigirCambioContrasenaFilter>();
    // CSRF/antiforgery obligatorio en todo POST/PUT/PATCH/DELETE (GET/HEAD/OPTIONS/TRACE
    // quedan afuera automáticamente). wwwroot/js/http.js manda el token en el header
    // configurado abajo; las vistas de Account que postean como formulario HTML
    // (Layout = null) lo mandan como campo oculto vía @Html.AntiForgeryToken().
    opt.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizedHttpMessageHandler>();

// AuthService no lleva el handler de autorización: todavía no hay token cuando se hace login.
builder.Services.AddHttpClient<IAuthService, AuthService>(c => c.BaseAddress = new Uri(apiBaseUrl));

// El resto de los Services sí necesitan mandar el JWT en cada llamada.
builder.Services.AddHttpClient<ICanchaService, CanchaService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IProductoService, ProductoService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IMaterialService, MaterialService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IVentaService, VentaService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IHorarioService, HorarioService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IReservaService, ReservaService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IEstadisticaService, EstadisticaService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IEstadisticaCanchaService, EstadisticaCanchaService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IUsuarioService, UsuarioService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();
builder.Services.AddHttpClient<IAuditoriaService, AuditoriaService>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(30); // Requerimiento no funcional §5.2.2 del documento original
});

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

// La API confía en el header "X-Cliente-IP" (ver comentario en FutbolYaAPI/Program.cs, rate
// limiting) cuando la conexión física le llega desde loopback, o cuando el header
// "X-Clave-Interna" trae el valor configurado en "ClaveInterna". Si ApiBaseUrl apunta a otro
// host y no hay "ClaveInterna" configurada, ninguna de las dos condiciones se cumple: el rate
// limiting de login/recuperación degradaría a compartir un único límite entre todos los
// usuarios del navegador (la API vería la IP de MVC, no la del cliente real). Avisamos al
// arrancar en vez de fallar, porque no es un error fatal.
if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
{
    var esLocal = Uri.CheckHostName(apiUri.Host) != UriHostNameType.Unknown &&
        (apiUri.IsLoopback || apiUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));

    if (!esLocal && string.IsNullOrEmpty(claveInterna))
    {
        app.Logger.LogWarning(
            "ApiBaseUrl ('{ApiBaseUrl}') no apunta a localhost/loopback y no hay 'ClaveInterna' configurada. " +
            "La API no va a confiar en el header X-Cliente-IP, así que el rate limiting de login/recuperación " +
            "va a degradar a un único límite compartido para todos los usuarios (ver comentario de rate " +
            "limiting en FutbolYaAPI/Program.cs). Configurá 'ClaveInterna' con el mismo valor en MVC y en la API.",
            apiBaseUrl);
    }
}

// Primero de todo el pipeline: hasta que corre, HttpContext.Connection.RemoteIpAddress y
// Request.Scheme siguen mostrando el proxy de App Service en vez del cliente/esquema real.
app.UseForwardedHeaders();

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
