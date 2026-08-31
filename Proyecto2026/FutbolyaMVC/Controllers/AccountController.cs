using Microsoft.AspNetCore.Mvc;
using FutbolyaMVC.DTOs;
using FutbolyaMVC.Services;

namespace FutbolyaMVC.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    // GET: /Account/Login
    // Muestra el formulario de login
    public IActionResult Login()
    {
        return View();
    }

    // POST: /Account/Login
    // Procesa el login
    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // Llamamos al servicio para autenticar
        var resultado = await _authService.LoginAsync(request);

        // Si el login falla
        if (resultado == null)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos");
            return View();
        }

        // Si el login es exitoso, guardamos datos en sesión
        HttpContext.Session.SetInt32("Cod_Usuario", resultado.Cod_Usuario);
        HttpContext.Session.SetString("Nombre", resultado.Nombre);
        HttpContext.Session.SetInt32("Rol", resultado.Rol ? 1 : 0);

        // Redirigimos al dashboard/menú
        return RedirectToAction("Index", "Home");
    }
    // GET: /Account/Logout
    // Cierra la sesión del usuario
    public IActionResult Logout()
    {
        // Limpiamos la sesión
        HttpContext.Session.Clear();

        // Redirigimos al login
        return RedirectToAction("Login");
    }
}