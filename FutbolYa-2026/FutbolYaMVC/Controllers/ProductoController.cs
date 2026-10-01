using Microsoft.AspNetCore.Mvc;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Services;

namespace FutbolYaMVC.Controllers;

// CRUD de productos del punto de venta y ajuste de su stock. Accesible a cualquier usuario
// logueado; llama a IProductoService. Las acciones de escritura devuelven JSON para productos.js.
public class ProductoController : Controller
{
    private static readonly string[] TiposValidos = { "Bebida", "Comida" };

    private readonly IProductoService _productoService;

    public ProductoController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    // GET: /Producto/Index
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        ViewData["Rol"] = HttpContext.Session.GetInt32("Rol");
        ViewData["Nombre"] = HttpContext.Session.GetString("Nombre");

        var productos = await _productoService.GetAllAsync();

        // Re-chequeo tras las llamadas a la API: ver comentario equivalente en ReservaController.Index.
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return RedirectToAction("Login", "Account");

        return View(productos);
    }

    // POST: /Producto/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductoCreateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var error = Validar(request);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (creado, errorApi) = await _productoService.CreateAsync(request);
        if (creado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo crear el producto" });

        return Ok(creado);
    }

    // POST: /Producto/Edit/5
    // No recibe/edita el stock (ver ProductoUpdateRequest); usar AjustarStock.
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromBody] ProductoUpdateRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var error = ValidarEdicion(request);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var (actualizado, errorApi) = await _productoService.UpdateAsync(id, request);
        if (actualizado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo editar el producto" });

        return Ok(actualizado);
    }

    // POST: /Producto/AjustarStock/5
    // Ajuste relativo de stock (+n ingreso / -n egreso), independiente de la edición.
    [HttpPost]
    public async Task<IActionResult> AjustarStock(int id, [FromBody] AjustarStockRequest request)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        if (request.Ajuste == 0)
            return BadRequest(new { mensaje = "El ajuste debe ser distinto de 0" });

        var (actualizado, errorApi) = await _productoService.AjustarStockAsync(id, request.Ajuste);
        if (actualizado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo ajustar el stock" });

        return Ok(actualizado);
    }

    // POST: /Producto/Reactivar/5
    [HttpPost]
    public async Task<IActionResult> Reactivar(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var (reactivado, errorApi) = await _productoService.ReactivarAsync(id);
        if (reactivado == null)
            return BadRequest(new { mensaje = errorApi ?? "No se pudo reactivar el producto" });

        return Ok(reactivado);
    }

    // POST: /Producto/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (HttpContext.Session.GetInt32("Cod_Usuario") == null)
            return Unauthorized();

        var resultado = await _productoService.DeleteAsync(id);
        if (!resultado.Exitoso)
            return BadRequest(new { mensaje = resultado.Mensaje });

        return Ok(new { mensaje = resultado.Mensaje ?? "Producto eliminado correctamente" });
    }

    private static string? Validar(ProductoCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return "El nombre es obligatorio";

        if (request.Cantidad < 0)
            return "El stock no puede ser negativo";

        if (request.Precio < 0)
            return "El precio no puede ser negativo";

        if (!TiposValidos.Contains(request.Tipo, StringComparer.OrdinalIgnoreCase))
            return "El tipo debe ser 'Bebida' o 'Comida'";

        return null;
    }

    // Sin chequeo de stock — ProductoUpdateRequest ya no lo trae.
    private static string? ValidarEdicion(ProductoUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return "El nombre es obligatorio";

        if (request.Precio < 0)
            return "El precio no puede ser negativo";

        if (!TiposValidos.Contains(request.Tipo, StringComparer.OrdinalIgnoreCase))
            return "El tipo debe ser 'Bebida' o 'Comida'";

        return null;
    }
}
