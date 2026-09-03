using Microsoft.AspNetCore.Mvc;
using FutbolyaMVC.DTOs;
using FutbolyaMVC.Services;

namespace FutbolyaMVC.Controllers;

public class MaterialController : Controller
{
    private readonly IMaterialService _materialService;

    public MaterialController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    // GET: /Material/Index
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var materiales = await _materialService.GetAllAsync();
        return View(materiales);
    }

    // POST: /Material/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MaterialCreateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var error = Validar(request);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (creado, errorApi) = await _materialService.CreateAsync(request);
        if (creado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo crear el material" });

        return Ok(creado);
    }

    // POST: /Material/Edit/5
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromBody] MaterialCreateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var error = Validar(request);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (actualizado, errorApi) = await _materialService.UpdateAsync(id, request);
        if (actualizado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo editar el material" });

        return Ok(actualizado);
    }

    // POST: /Material/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var resultado = await _materialService.DeleteAsync(id);
        if (!resultado.Exitoso)
            return BadRequest(new { mensaje = resultado.Mensaje });

        return Ok(new { mensaje = resultado.Mensaje ?? "Material eliminado correctamente" });
    }

    private static string? Validar(MaterialCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return "El nombre es obligatorio";

        if (request.Cant_Material < 0)
            return "El stock no puede ser negativo";

        return null;
    }
}
