using System.Security.Claims;

namespace FutbolYaAPI.Seguridad;

// Helpers para leer datos del usuario autenticado desde el ClaimsPrincipal que arma el
// middleware de JWT, usados por los endpoints para saber quién hace la request.
public static class ClaimsPrincipalExtensions
{
    /// <summary>Obtiene el Cod_Usuario del token JWT del usuario autenticado (usado para Auditoria).</summary>
    public static int CodUsuario(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, out var id) ? id : 0;
    }
}
