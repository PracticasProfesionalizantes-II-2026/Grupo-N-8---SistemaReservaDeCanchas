using Microsoft.AspNetCore.Mvc;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Services;

namespace FutbolYaMVC.Controllers;

// Reportes de ventas y de reservas por cancha (día/semana/mes), solo para administradores.
// Las acciones de reporte son GET consumidos por AJAX y llaman a IEstadisticaService /
// IEstadisticaCanchaService.
public class EstadisticaController : Controller
{
    private readonly IEstadisticaService _estadisticaService;
    private readonly IEstadisticaCanchaService _estadisticaCanchaService;
    private readonly IProductoService _productoService;
    private readonly ICanchaService _canchaService;

    public EstadisticaController(
        IEstadisticaService estadisticaService,
        IEstadisticaCanchaService estadisticaCanchaService,
        IProductoService productoService,
        ICanchaService canchaService)
    {
        _estadisticaService = estadisticaService;
        _estadisticaCanchaService = estadisticaCanchaService;
        _productoService = productoService;
        _canchaService = canchaService;
    }

    // GET: /Estadistica/Index
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        // Módulo restringido a administradores (Rol == 1), igual que en el menú lateral.
        if (HttpContext.Session.GetInt32("Rol") != 1)
            return RedirectToAction("Index", "Reserva");

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var productos = await _productoService.GetAllAsync();
        var canchas = await _canchaService.GetAllAsync();

        // Re-chequeo tras las llamadas a la API: ver comentario equivalente en ReservaController.Index.
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        return View(new EstadisticaIndexViewModel(productos, canchas));
    }

    // ── Reporte de Ventas (AJAX) ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Dia(DateTime? fecha, string? productos)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (!TryParseIds(productos, out var idsProductos))
            return BadRequest(new { mensaje = MensajeIdsInvalidos });

        var resultado = await _estadisticaService.ObtenerDia(fecha ?? DateTime.Today, idsProductos);
        if (resultado == null)
            return BadRequest(new { mensaje = "No se pudo obtener la estadística del día" });

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Semana(DateTime? fecha, string? productos)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (!TryParseIds(productos, out var idsProductos))
            return BadRequest(new { mensaje = MensajeIdsInvalidos });

        var resultado = await _estadisticaService.ObtenerSemana(fecha ?? DateTime.Today, idsProductos);
        if (resultado == null)
            return BadRequest(new { mensaje = "No se pudo obtener la estadística de la semana" });

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Mes(DateTime? fecha, string? productos)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (!TryParseIds(productos, out var idsProductos))
            return BadRequest(new { mensaje = MensajeIdsInvalidos });

        var resultado = await _estadisticaService.ObtenerMes(fecha ?? DateTime.Today, idsProductos);
        if (resultado == null)
            return BadRequest(new { mensaje = "No se pudo obtener la estadística del mes" });

        return Ok(resultado);
    }

    // ── Reporte de Reserva de Cancha (AJAX) ──────────────────────────────

    [HttpGet]
    public async Task<IActionResult> DiaCancha(DateTime? fecha, string? canchas)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (!TryParseIds(canchas, out var idsCanchas))
            return BadRequest(new { mensaje = MensajeIdsInvalidos });

        var resultado = await _estadisticaCanchaService.ObtenerDia(fecha ?? DateTime.Today, idsCanchas);
        if (resultado == null)
            return BadRequest(new { mensaje = "No se pudo obtener la estadística del día" });

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> SemanaCancha(DateTime? fecha, string? canchas)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (!TryParseIds(canchas, out var idsCanchas))
            return BadRequest(new { mensaje = MensajeIdsInvalidos });

        var resultado = await _estadisticaCanchaService.ObtenerSemana(fecha ?? DateTime.Today, idsCanchas);
        if (resultado == null)
            return BadRequest(new { mensaje = "No se pudo obtener la estadística de la semana" });

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> MesCancha(DateTime? fecha, string? canchas)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (!TryParseIds(canchas, out var idsCanchas))
            return BadRequest(new { mensaje = MensajeIdsInvalidos });

        var resultado = await _estadisticaCanchaService.ObtenerMes(fecha ?? DateTime.Today, idsCanchas);
        if (resultado == null)
            return BadRequest(new { mensaje = "No se pudo obtener la estadística del mes" });

        return Ok(resultado);
    }

    private const string MensajeIdsInvalidos = "Los filtros seleccionados no son válidos";

    // Un id mal escrito en la URL responde 400 con un mensaje claro en vez de un 500
    // (mismo criterio que la API).
    private static bool TryParseIds(string? valores, out List<int>? ids)
    {
        ids = null;
        if (string.IsNullOrWhiteSpace(valores))
            return true;

        var lista = new List<int>();
        foreach (var parte in valores.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(parte, out var id))
                return false;
            lista.Add(id);
        }

        ids = lista;
        return true;
    }
}
