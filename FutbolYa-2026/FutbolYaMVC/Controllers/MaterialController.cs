using Microsoft.AspNetCore.Mvc;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Services;

namespace FutbolYaMVC.Controllers;

// CRUD de materiales deportivos y ajuste de su stock. Accesible a cualquier usuario logueado;
// llama a IMaterialService. Las acciones de escritura devuelven JSON para materiales.js.
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

        // Re-chequeo tras las llamadas a la API: ver comentario equivalente en ReservaController.Index.
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

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
    // No recibe/edita el stock (ver MaterialUpdateRequest); usar AjustarStock.
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromBody] MaterialUpdateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest(new { mensaje = "El nombre es obligatorio" });

        var (actualizado, errorApi) = await _materialService.UpdateAsync(id, request);
        if (actualizado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo editar el material" });

        return Ok(actualizado);
    }

    // POST: /Material/AjustarStock/5
    // Ajuste relativo de stock (+n ingreso / -n egreso), independiente de la edición.
    [HttpPost]
    public async Task<IActionResult> AjustarStock(int id, [FromBody] AjustarStockRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (request.Ajuste == 0)
            return BadRequest(new { mensaje = "El ajuste debe ser distinto de 0" });

        var (actualizado, errorApi) = await _materialService.AjustarStockAsync(id, request.Ajuste);
        if (actualizado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo ajustar el stock" });

        return Ok(actualizado);
    }

    // POST: /Material/Reactivar/5
    [HttpPost]
    public async Task<IActionResult> Reactivar(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var (reactivado, errorApi) = await _materialService.ReactivarAsync(id);
        if (reactivado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo reactivar el material" });

        return Ok(reactivado);
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
