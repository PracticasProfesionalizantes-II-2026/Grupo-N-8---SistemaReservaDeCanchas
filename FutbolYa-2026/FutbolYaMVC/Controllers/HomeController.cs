using Microsoft.AspNetCore.Mvc;

namespace FutbolYaMVC.Controllers;

// Dashboard mostrado tras el login; accesible a cualquier usuario logueado.
public class HomeController : Controller
{
    // GET: /Home/Index
    public IActionResult Index()
    {
        var codUsuario = HttpContext.Session.GetInt32("Cod_Usuario");
        if (codUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var rol = HttpContext.Session.GetInt32("Rol");
        ViewData["Rol"] = rol;
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        return View();
    }
}