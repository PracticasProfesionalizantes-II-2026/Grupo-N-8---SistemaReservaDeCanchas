using Microsoft.AspNetCore.Mvc;

namespace FutbolyaMVC.Controllers;

public class HomeController : Controller
{
    // GET: /Home/Index
    // Dashboard - Menú principal después del login
    public IActionResult Index()
    {
        // Verificamos si el usuario está logueado
        var codUsuario = HttpContext.Session.GetInt32("Cod_Usuario");
        if (codUsuario == null)
        {
            // Si no está logueado, lo redirige al login
            return RedirectToAction("Login", "Account");
        }

        // Pasamos el Rol a la vista para que muestre el menú correcto
        var rol = HttpContext.Session.GetInt32("Rol");
        ViewData["Rol"] = rol;
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        return View();
    }
}