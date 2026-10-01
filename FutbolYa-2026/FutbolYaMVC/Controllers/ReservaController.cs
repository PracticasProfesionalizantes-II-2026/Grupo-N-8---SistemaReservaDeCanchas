using Microsoft.AspNetCore.Mvc;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Services;

namespace FutbolYaMVC.Controllers;

// Alta, consulta y cancelación de reservas de cancha. Accesible a cualquier usuario logueado;
// llama a IReservaService/ICanchaService/IHorarioService/IMaterialService. Las acciones de
// escritura devuelven JSON para reserva.js.
public class ReservaController : Controller
{
    private readonly IReservaService _reservaService;
    private readonly ICanchaService _canchaService;
    private readonly IHorarioService _horarioService;
    private readonly IMaterialService _materialService;

    public ReservaController(
        IReservaService reservaService,
        ICanchaService canchaService,
        IHorarioService horarioService,
        IMaterialService materialService)
    {
        _reservaService = reservaService;
        _canchaService = canchaService;
        _horarioService = horarioService;
        _materialService = materialService;
    }

    // GET: /Reserva/Index
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var reservas = await _reservaService.GetAllAsync();
        var canchas = await _canchaService.GetAllAsync();
        var horarios = await _horarioService.GetAllAsync();
        var materiales = await _materialService.GetAllAsync();

        // Si alguna de las llamadas de arriba recibió un 401 de la API,
        // AuthorizedHttpMessageHandler ya limpió la sesión — hay que revisar de nuevo en vez
        // de renderizar la página con listas vacías como si no hubiera datos.
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        return View(new ReservaIndexViewModel(reservas, canchas, horarios, materiales));
    }

    // GET: /Reserva/Details/5  (AJAX, por si se necesita refrescar el detalle de una reserva)
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var reserva = await _reservaService.GetByIdAsync(id);
        if (reserva == null)
            return NotFound(new { mensaje = "Reserva no encontrada", cod_reserva = id });

        return Ok(reserva);
    }

    // POST: /Reserva/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReservaCreateRequest request)
    {
        var codUsuario = HttpContext.Session.GetInt32("Cod_Usuario");
        if (codUsuario == null)
            return Unauthorized();

        var error = Validar(request);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (creada, errorApi) = await _reservaService.CreateAsync(codUsuario.Value, request);
        if (creada == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo registrar la reserva" });

        return Ok(creada);
    }

    // POST: /Reserva/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var resultado = await _reservaService.DeleteAsync(id);
        if (!resultado.Exitoso)
            return BadRequest(new { mensaje = resultado.Mensaje });

        // Se devuelve la reserva actualizada (ya en estado Cancelada) para que la fila se
        // refresque en el listado sin recargar la página, en vez de simplemente desaparecer.
        var reserva = await _reservaService.GetByIdAsync(id);
        return Ok(new { mensaje = resultado.Mensaje ?? "Reserva cancelada correctamente", reserva });
    }

    private static string? Validar(ReservaCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Dni_Cliente))
            return "El DNI del cliente es obligatorio";

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Dni_Cliente, @"^\d{7,8}$"))
            return "Dni inválido.";

        if (string.IsNullOrWhiteSpace(request.Telefono_Cliente))
            return "El teléfono del cliente es obligatorio";

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Telefono_Cliente, @"^\d{6,15}$"))
            return "Teléfono inválido.";

        if (request.Cod_Cancha <= 0)
            return "Seleccioná una cancha";

        if (request.Cod_Horarios == null || request.Cod_Horarios.Count == 0)
            return "Seleccioná al menos un horario";

        if (request.Materiales != null && request.Materiales.Any(m => m.Cantidad <= 0))
            return "La cantidad de cada material debe ser mayor a 0";

        return null;
    }
}
