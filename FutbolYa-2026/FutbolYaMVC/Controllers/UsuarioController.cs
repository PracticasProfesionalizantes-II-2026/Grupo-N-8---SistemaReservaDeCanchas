using Microsoft.AspNetCore.Mvc;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Services;

namespace FutbolYaMVC.Controllers;

// Gestión de usuarios del sistema y su historial de auditoría. Todo el módulo es
// solo-Administrador; llama a IUsuarioService/IAuditoriaService.
public class UsuarioController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly IAuditoriaService _auditoriaService;

    public UsuarioController(IUsuarioService usuarioService, IAuditoriaService auditoriaService)
    {
        _usuarioService = usuarioService;
        _auditoriaService = auditoriaService;
    }

    private bool EsAdministrador() => HttpContext.Session.GetInt32("Rol") == 1;

    // GET: /Usuario/Index
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        if (!EsAdministrador())
            return RedirectToAction("Index", "Reserva");

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var usuarios = await _usuarioService.GetAllAsync();

        // Re-chequeo tras las llamadas a la API: ver comentario equivalente en ReservaController.Index.
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        return View(usuarios);
    }

    // GET: /Usuario/Auditoria
    // Historial de alta/modificación/baja. Solo lectura, solo Administrador.
    public async Task<IActionResult> Auditoria()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        if (!EsAdministrador())
            return RedirectToAction("Index", "Reserva");

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var auditorias = await _auditoriaService.GetAllAsync();
        var usuarios = await _usuarioService.GetAllAsync();

        // Re-chequeo tras las llamadas a la API: ver comentario equivalente en ReservaController.Index.
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        return View(new AuditoriaIndexViewModel(auditorias, usuarios));
    }

    // POST: /Usuario/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateRequest request)
    {
        if (!EsAdministrador())
            return Forbid();

        var error = Validar(request.Nombre, request.Apellido, request.Dni, request.Correo, request.Contraseña);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (usuario, errorApi, codUsuarioInactivo) = await _usuarioService.CreateAsync(request);
        if (usuario == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo crear el usuario", cod_usuario_inactivo = codUsuarioInactivo });

        return Ok(usuario);
    }

    // POST: /Usuario/Reactivar/5
    // Reactiva un usuario dado de baja con los datos nuevos, en vez de dejar crear
    // un usuario duplicado con el mismo DNI/correo.
    [HttpPost]
    public async Task<IActionResult> Reactivar(int id, [FromBody] UsuarioCreateRequest request)
    {
        if (!EsAdministrador())
            return Forbid();

        var error = Validar(request.Nombre, request.Apellido, request.Dni, request.Correo, request.Contraseña);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (usuario, errorApi) = await _usuarioService.ReactivarAsync(id, request);
        if (usuario == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo reactivar el usuario" });

        return Ok(usuario);
    }

    // POST: /Usuario/Edit/5
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromBody] UsuarioUpdateRequest request)
    {
        if (!EsAdministrador())
            return Forbid();

        var error = Validar(request.Nombre, request.Apellido, request.Dni, request.Correo, null);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (usuario, errorApi) = await _usuarioService.UpdateAsync(id, request);
        if (usuario == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo editar el usuario" });

        return Ok(usuario);
    }

    // POST: /Usuario/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!EsAdministrador())
            return Forbid();

        var resultado = await _usuarioService.DeleteAsync(id);
        if (!resultado.Exitoso)
            return BadRequest(new { mensaje = resultado.Mensaje });

        return Ok(new { mensaje = resultado.Mensaje ?? "Usuario eliminado correctamente" });
    }

    // POST: /Usuario/ResetearContrasena/5
    [HttpPost]
    public async Task<IActionResult> ResetearContrasena(int id)
    {
        if (!EsAdministrador())
            return Forbid();

        var (exitoso, mensaje) = await _usuarioService.ResetearContrasenaAsync(id);
        if (!exitoso)
            return BadRequest(new { mensaje });

        return Ok(new { mensaje });
    }

    private static string? Validar(string nombre, string apellido, string dni, string correo, string? contraseña)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(correo))
            return "Todos los campos son obligatorios";

        if (!System.Text.RegularExpressions.Regex.IsMatch(dni, @"^\d{7,8}$"))
            return "El DNI debe tener entre 7 y 8 dígitos numéricos";

        if (!System.Text.RegularExpressions.Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return "El formato del correo no es válido";

        if (contraseña != null && contraseña.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres";

        return null;
    }
}
