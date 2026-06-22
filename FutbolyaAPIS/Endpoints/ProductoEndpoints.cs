using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class ProductoEndpoints
{
    public static void MapProductoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/productos").WithTags("Productos");
        // ── GET /api/productos ─────────────────────────────────────────
        group.MapGet("/", async (
            IProductoLogica logica,
            string? tipo,
            string? nombre) =>
        {
            try
            {
                var productos = await logica.ObtenerTodos();

                if (!string.IsNullOrWhiteSpace(tipo))
                    productos = productos.Where(p =>
                        p.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(nombre))
                    productos = productos.Where(p =>
                        p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));

                return Results.Ok(productos);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/productos/{id} ────────────────────────────────────
        group.MapGet("/{id:int}", async (int id, IProductoLogica logica) =>
        {
            try
            {
                var producto = await logica.ObtenerPorId(id);
                if (producto is null)
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                return Results.Ok(producto);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/productos ────────────────────────────────────────
        group.MapPost("/", async (ProductoCreateDto dto, IProductoLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Tipo))
                    return Results.BadRequest(new { mensaje = "Nombre y Tipo son obligatorios" });

                var id = await logica.Crear(dto);
                var creado = await logica.ObtenerPorId(id);

                return Results.Created($"/api/productos/{id}", creado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PUT /api/productos/{id} ────────────────────────────────────
        group.MapPut("/{id:int}", async (int id, ProductoCreateDto dto, IProductoLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Tipo))
                    return Results.BadRequest(new { mensaje = "Nombre y Tipo son obligatorios" });

                var actualizado = await logica.Actualizar(id, dto);
                if (!actualizado)
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                var producto = await logica.ObtenerPorId(id);
                return Results.Ok(producto);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/productos/{id}/stock ───────────────────────────
        group.MapPatch("/{id:int}/stock", async (int id, StockUpdateDto dto, IProductoLogica logica) =>
        {
            try
            {
                if (dto.Cantidad < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                var actualizado = await logica.ActualizarStock(id, dto.Cantidad);
                if (!actualizado)
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                var producto = await logica.ObtenerPorId(id);
                return Results.Ok(producto);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/productos/{id} ────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, IProductoLogica logica) =>
        {
            try
            {
                var producto = await logica.ObtenerPorId(id);
                if (producto is null)
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                var eliminado = await logica.Eliminar(id);
                if (!eliminado)
                    return Results.Conflict(new { mensaje = "No se pudo eliminar el producto, puede tener ventas asociadas" });

                return Results.Ok(new { mensaje = "Producto eliminado correctamente", cod_producto = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}