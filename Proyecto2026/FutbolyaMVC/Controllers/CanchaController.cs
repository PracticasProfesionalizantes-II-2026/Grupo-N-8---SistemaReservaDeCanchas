using Microsoft.AspNetCore.Mvc;
using FutbolyaMVC.DTOs;
using FutbolyaMVC.Services;

namespace FutbolyaMVC.Controllers;

public class CanchaController : Controller
{
    private readonly ICanchaService _canchaService;

    public CanchaController(ICanchaService canchaService)
    {
        _canchaService = canchaService;
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
        return View(canchas);
    }

    // POST: /Cancha/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CanchaCreateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest(new { mensaje = "La descripción es obligatoria" });

        var creada = await _canchaService.CreateAsync(request);
        if (creada == null)
            return BadRequest(new { mensaje = "No se pudo crear la cancha" });

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