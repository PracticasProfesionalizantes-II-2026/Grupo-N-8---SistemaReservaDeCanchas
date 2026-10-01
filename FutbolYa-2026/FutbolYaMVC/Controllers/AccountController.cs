using Microsoft.AspNetCore.Mvc;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Services;

namespace FutbolYaMVC.Controllers;

// Login/logout, cambio de contraseña obligatorio y recuperación por código. Acciones anónimas
// (no requieren sesión); llama a IAuthService/IUsuarioService contra la API y arma la sesión
// (Cod_Usuario, Rol, Jwt, DebeCambiarContrasena) al autenticar.
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUsuarioService _usuarioService;

    public AccountController(IAuthService authService, IUsuarioService usuarioService)
    {
        _authService = authService;
        _usuarioService = usuarioService;
    }

    // GET: /Account/Login
    // ?expirada=1 lo agrega wwwroot/js/http.js cuando detecta una sesión vencida (401 de la
    // API) o un token antiforgery caducado por inactividad larga.
    public IActionResult Login(string? expirada)
    {
        if (expirada == "1")
            ViewBag.MensajeExpirada = "Tu sesión expiró. Iniciá sesión nuevamente.";

        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (resultado, mensaje) = await _authService.LoginAsync(request);

        if (resultado == null)
        {
            // Si el rate limiter de la API devolvió 429, mensaje trae el texto real
            // ("Demasiados intentos...") en vez del genérico de credenciales.
            ModelState.AddModelError("", mensaje ?? "Correo o contraseña incorrectos");
            return View();
        }

        HttpContext.Session.SetInt32("Cod_Usuario", resultado.Cod_Usuario);
        HttpContext.Session.SetString("Nombre", resultado.Nombre);
        HttpContext.Session.SetInt32("Rol", resultado.Rol ? 1 : 0);
        HttpContext.Session.SetString("Jwt", resultado.Token);
        HttpContext.Session.SetInt32("DebeCambiarContrasena", resultado.Cambiar_Contraseña ? 1 : 0);

        // Si tiene que cambiar la contraseña, no lo dejamos entrar al resto del sistema.
        if (resultado.Cambiar_Contraseña)
            return RedirectToAction("CambiarContrasena");

        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/CambiarContrasena
    public IActionResult CambiarContrasena()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login");

        return View();
    }

    // POST: /Account/CambiarContrasena
    [HttpPost]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaRequest request)
    {
        var codUsuario = HttpContext.Session.GetInt32("Cod_Usuario");
        if (codUsuario == null)
            return RedirectToAction("Login");

        if (request.Contrasena_Nueva != request.Contrasena_Nueva_Confirmacion)
        {
            ModelState.AddModelError("", "La nueva contraseña y su confirmación no coinciden");
            return View();
        }

        var (exitoso, mensaje) = await _usuarioService.CambiarContrasenaAsync(codUsuario.Value, request);
        if (!exitoso)
        {
            ModelState.AddModelError("", mensaje ?? "No se pudo cambiar la contraseña");
            return View();
        }

        HttpContext.Session.SetInt32("DebeCambiarContrasena", 0);
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/OlvideContrasena
    // Paso 1 del flujo "¿Olvidé mi contraseña?" desde el login.
    public IActionResult OlvideContrasena()
    {
        return View();
    }

    // POST: /Account/OlvideContrasena
    [HttpPost]
    public async Task<IActionResult> OlvideContrasena(string correo)
    {
        var (exitoso, mensaje) = await _authService.SolicitarRecuperacionAsync(correo);

        // Si el servidor devolvió error (ej. falló el envío del correo), lo mostramos tal cual
        // en vez de tapar el problema con el mensaje genérico de "puede que no esté registrado".
        ViewBag.Mensaje = exitoso
            ? (mensaje ?? "Si el correo está registrado, vas a recibir un código de verificación.")
            : (mensaje ?? "No se pudo procesar la solicitud. Intentá de nuevo en unos minutos.");
        ViewBag.Correo = correo;
        return View("ConfirmarRecuperacion");
    }

    // GET: /Account/ConfirmarRecuperacion
    // Entrada directa para cuando ya se tiene un código (ej. el que manda un admin desde el
    // módulo de Usuarios): evita tener que pasar por OlvideContrasena, que siempre genera
    // y manda un código nuevo, invalidando el que ya se tenía.
    public IActionResult ConfirmarRecuperacion()
    {
        return View();
    }

    // POST: /Account/ConfirmarRecuperacion
    // Paso 2: el usuario ingresa el código recibido por correo y su nueva contraseña.
    [HttpPost]
    public async Task<IActionResult> ConfirmarRecuperacion(ConfirmarRecuperacionRequest request)
    {
        if (request.Contrasena_Nueva != request.Contrasena_Nueva_Confirmacion)
        {
            ModelState.AddModelError("", "La nueva contraseña y su confirmación no coinciden");
            ViewBag.Correo = request.Correo;
            return View();
        }

        var (exitoso, mensaje) = await _authService.ConfirmarRecuperacionAsync(request);
        if (!exitoso)
        {
            ModelState.AddModelError("", mensaje ?? "No se pudo actualizar la contraseña");
            ViewBag.Correo = request.Correo;
            return View();
        }

        TempData["MensajeLogin"] = "Contraseña actualizada correctamente. Ya podés iniciar sesión.";
        return RedirectToAction("Login");
    }

    // GET: /Account/Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
