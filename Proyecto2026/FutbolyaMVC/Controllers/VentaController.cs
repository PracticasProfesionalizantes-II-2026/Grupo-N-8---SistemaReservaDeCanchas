using Microsoft.AspNetCore.Mvc;
using FutbolyaMVC.DTOs;
using FutbolyaMVC.Services;

namespace FutbolyaMVC.Controllers;

public class VentaController : Controller
{
    private readonly IVentaService _ventaService;
    private readonly IProductoService _productoService;

    public VentaController(IVentaService ventaService, IProductoService productoService)
    {
        _ventaService = ventaService;
        _productoService = productoService;
    }

    // GET: /Venta/Index
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var ventas = await _ventaService.GetAllAsync();
        var productos = await _productoService.GetAllAsync();

        return View(new VentaIndexViewModel(ventas, productos));
    }

    // GET: /Venta/Details/5  (AJAX, alimenta el modal "Ver Detalles")
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var venta = await _ventaService.GetByIdAsync(id);
        if (venta == null)
            return NotFound(new { mensaje = "Venta no encontrada", cod_venta = id });

        return Ok(venta);
    }

    // POST: /Venta/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VentaCreateRequest request)
    {
        var codUsuario = HttpContext.Session.GetInt32("Cod_Usuario");
        if (codUsuario == null)
            return Unauthorized();

        if (request.Detalle == null || request.Detalle.Count == 0)
            return BadRequest(new { mensaje = "La venta debe tener al menos un producto" });

        if (request.Detalle.Any(d => d.Cantidad <= 0))
            return BadRequest(new { mensaje = "La cantidad de cada producto debe ser mayor a 0" });

        var (creada, errorApi) = await _ventaService.CreateAsync(codUsuario.Value, request);
        if (creada == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo registrar la venta" });

        return Ok(creada);
    }

    // POST: /Venta/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var resultado = await _ventaService.DeleteAsync(id);
        if (!resultado.Exitoso)
            return BadRequest(new { mensaje = resultado.Mensaje });

        return Ok(new { mensaje = resultado.Mensaje ?? "Venta eliminada correctamente" });
    }
}
