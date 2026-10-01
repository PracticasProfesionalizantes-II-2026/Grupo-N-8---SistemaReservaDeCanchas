using System.Net;
using System.Net.Http.Headers;

namespace FutbolYaMVC.Infraestructura;

/// <summary>
/// Adjunta automáticamente el JWT guardado en sesión (tras el login) a toda llamada
/// que los Services le hagan a FutbolYaAPI.
/// </summary>
public class AuthorizedHttpMessageHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AuthorizedHttpMessageHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString("Jwt");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Estos clientes no pegan contra /api/auth/* (sin rate limiting), así que estos headers
        // son inofensivos hoy — se mandan igual por si en el futuro se agrega algún límite
        // particionado por IP fuera de auth. Ver AuthService para el caso real.
        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
        {
            request.Headers.TryAddWithoutValidation("X-Cliente-IP", ip);
        }

        var claveInterna = _configuration["ClaveInterna"];
        if (!string.IsNullOrEmpty(claveInterna))
        {
            request.Headers.TryAddWithoutValidation("X-Clave-Interna", claveInterna);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Detección centralizada de "la API ya no reconoce esta sesión" — el JWT expiró
        // naturalmente o quedó invalidado por la revalidación contra la DB (usuario
        // desactivado/eliminado, rol cambiado). Limpiando la sesión acá, el próximo chequeo de
        // Session.GetInt32("Cod_Usuario") en cualquier controller/Index vuelve a fallar y
        // redirige. No confundir con un 401 propio del controller MVC (sesión ya ausente
        // desde el vamos): ahí no hay nada que limpiar.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }

        return response;
    }
}
