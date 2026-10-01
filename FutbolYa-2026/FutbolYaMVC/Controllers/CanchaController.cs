using Microsoft.AspNetCore.Mvc;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Services;

namespace FutbolYaMVC.Controllers;

// CRUD de canchas, su estado (Disponible/Mantenimiento/Baja) y sus horarios ofrecidos.
// Accesible a cualquier usuario logueado; llama a ICanchaService/IHorarioService. Las acciones
// de escritura devuelven JSON para el JS de la página (wwwroot/js/cancha.js).
public class CanchaController : Controller
{
    private readonly ICanchaService _canchaService;
    private readonly IHorarioService _horarioService;

    public CanchaController(ICanchaService canchaService, IHorarioService horarioService)
    {
        _canchaService = canchaService;
        _horarioService = horarioService;
    }

    // GET: /Cancha/Index
    public async Task<IActionResult> Index()
    {
        var codUsuario = HttpContext.Session.GetInt32("Cod_Usuario");
        if (codUsuario == null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var canchas = await _canchaService.GetAllAsync();
        var horarios = await _horarioService.GetAllAsync();

        // Re-chequeo tras las llamadas a la API: ver comentario equivalente en ReservaController.Index.
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        return View(new CanchaIndexViewModel(canchas, horarios));
    }

    // POST: /Cancha/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CanchaCreateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest(new { mensaje = "La descripción es obligatoria" });

        var (creada, error) = await _canchaService.CreateAsync(request);
        if (creada == null)
            return BadRequest(new { mensaje = error ?? "No se pudo crear la cancha" });

        return Ok(creada);
    }

    // POST: /Cancha/Edit/5
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromBody] CanchaUpdateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest(new { mensaje = "La descripción no puede estar vacía" });

        var actualizada = await _canchaService.UpdateAsync(id, request);
        if (actualizada == null)
            return BadRequest(new { mensaje = "No se pudo editar la cancha" });

        return Ok(actualizada);
    }

    // POST: /Cancha/EstadoUpdate/5
    [HttpPost]
    public async Task<IActionResult> EstadoUpdate(int id, [FromBody] CanchaEstadoUpdateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var estadosValidos = new[] { "Disponible", "Mantenimiento", "Baja" };
        if (!estadosValidos.Contains(request.Estado))
            return BadRequest(new { mensaje = "Estado inválido" });

        var (cancha, error) = await _canchaService.ActualizarEstadoAsync(id, request.Estado);
        if (cancha == null)
            return BadRequest(new { mensaje = error ?? "No se pudo actualizar el estado" });

        return Ok(cancha);
    }

    // POST: /Cancha/HorariosUpdate/5
    // Qué bloques del catálogo global de horarios ofrece esta cancha.
    [HttpPost]
    public async Task<IActionResult> HorariosUpdate(int id, [FromBody] CanchaHorariosUpdateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var (cancha, error) = await _canchaService.ActualizarHorariosAsync(id, request.Cod_Horarios ?? new List<int>());
        if (cancha == null)
            return BadRequest(new { mensaje = error ?? "No se pudieron actualizar los horarios" });

        return Ok(cancha);
    }

    // POST: /Cancha/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var resultado = await _canchaService.DeleteAsync(id);
        if (!resultado.Exitoso)
            return BadRequest(new { mensaje = resultado.Mensaje });

        return Ok(new { mensaje = resultado.Mensaje ?? "Cancha eliminada correctamente" });
    }
}
