using System.Net.Http.Json;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;

namespace FutbolYaMVC.Services;

// HttpClient hacia /api/auth/*, sin AuthorizedHttpMessageHandler (todavía no hay JWT antes de loguearse).
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    // AuthService no pasa por AuthorizedHttpMessageHandler, así que acá se mandan a mano la IP
    // real del navegador y la clave interna en headers propios. La API (FutbolYaAPI/Program.cs)
    // los usa para particionar el rate limiter del login/recuperación cuando ve que la conexión
    // le llega desde loopback (la MVC llamando server-to-server en el mismo host) o cuando la
    // clave interna coincide (MVC y API en hosts distintos) — ver el comentario ahí sobre el
    // límite de confianza de estos headers.
    private HttpRequestMessage NuevaSolicitudConIp<T>(HttpMethod metodo, string url, T contenido)
    {
        var solicitud = new HttpRequestMessage(metodo, url)
        {
            // Genérico explícito: evita que JsonContent.Create serialice por el tipo estático
            // "object" en vez del record real (LoginRequest, etc.) si algún día se llama con
            // una variable ya boxeada.
            Content = JsonContent.Create(contenido, options: JsonDefaults.Options)
        };

        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
            solicitud.Headers.TryAddWithoutValidation("X-Cliente-IP", ip);

        var claveInterna = _configuration["ClaveInterna"];
        if (!string.IsNullOrEmpty(claveInterna))
            solicitud.Headers.TryAddWithoutValidation("X-Clave-Interna", claveInterna);

        return solicitud;
    }

    // Se propaga el mensaje que manda la API (un 429 del rate limiter trae { "mensaje": ... })
    // para que el controller lo muestre tal cual en vez de un mensaje genérico de credenciales.
    public async Task<(LoginResponse? resultado, string? mensaje)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.SendAsync(NuevaSolicitudConIp(HttpMethod.Post, "/api/auth/login", request));
            if (!response.IsSuccessStatusCode)
                return (null, await LeerMensaje(response));

            var resultado = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonDefaults.Options);
            return (resultado, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en login: {ex.Message}");
            return (null, null);
        }
    }

    public async Task<(bool exitoso, string? mensaje)> SolicitarRecuperacionAsync(string correo)
    {
        try
        {
            var response = await _httpClient.SendAsync(NuevaSolicitudConIp(HttpMethod.Post, "/api/auth/solicitar-recuperacion", new SolicitarRecuperacionRequest(correo)));
            var mensaje = await LeerMensaje(response);
            return (response.IsSuccessStatusCode, mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    public async Task<(bool exitoso, string? mensaje)> ConfirmarRecuperacionAsync(ConfirmarRecuperacionRequest request)
    {
        try
        {
            var response = await _httpClient.SendAsync(NuevaSolicitudConIp(HttpMethod.Post, "/api/auth/confirmar-recuperacion", request));
            var mensaje = await LeerMensaje(response);
            return (response.IsSuccessStatusCode, mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    private static async Task<string?> LeerMensaje(HttpResponseMessage response)
    {
        try
        {
            var texto = await response.Content.ReadAsStringAsync();

            var cuerpo = System.Text.Json.JsonSerializer.Deserialize<MensajeResponse>(texto, JsonDefaults.Options);
            if (!string.IsNullOrWhiteSpace(cuerpo?.Mensaje))
                return cuerpo.Mensaje;

            // Results.Problem(ex.Message) del lado de la API devuelve un ProblemDetails, no un
            // MensajeResponse; ahí el detalle del error real está en "detail" o "title".
            using var doc = System.Text.Json.JsonDocument.Parse(texto);
            if (doc.RootElement.TryGetProperty("detail", out var detalle))
                return detalle.GetString();
            if (doc.RootElement.TryGetProperty("title", out var titulo))
                return titulo.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }
}
