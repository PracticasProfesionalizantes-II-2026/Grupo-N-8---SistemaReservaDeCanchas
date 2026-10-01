using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FutbolYaMVC.Infraestructura;

/// <summary>
/// Si el login devuelve Cambiar_Contraseña=true, este filtro global bloquea cualquier
/// pantalla (salvo el propio Account) hasta que el usuario cambie la contraseña temporal.
/// </summary>
public class ExigirCambioContrasenaFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var controlador = context.RouteData.Values["controller"]?.ToString();
        if (string.Equals(controlador, "Account", StringComparison.OrdinalIgnoreCase))
            return;

        var codUsuario = context.HttpContext.Session.GetInt32("Cod_Usuario");
        if (codUsuario == null)
            return; // el propio controlador ya redirige a Login si hace falta

        var debeCambiar = context.HttpContext.Session.GetInt32("DebeCambiarContrasena") == 1;
        if (debeCambiar)
        {
            context.Result = new RedirectToActionResult("CambiarContrasena", "Account", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
