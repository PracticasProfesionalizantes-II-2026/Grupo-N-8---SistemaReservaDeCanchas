using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Seguridad;
using System.Security.Claims;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/productos: catálogo del punto de venta (bebidas/comida). Capa fina:
// bind DTO → IProductoLogica → status code. El stock ya no se edita vía PUT (ver más abajo);
// solo se mueve con PATCH /{id}/stock, que es atómico y auditado.
public static class ProductoEndpoints
{
    public static void MapProductoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/productos").WithTags("Productos").RequireAuthorization();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();
        // ── GET /api/productos ─────────────────────────────────────────
        // Permite filtrar por tipo y/o nombre.
        group.MapGet("/", async (
            IProductoLogica logica,
            string? tipo,
            string? nombre,
            bool incluirBajas = false) =>
        {
            try
            {
                var productos = await logica.ObtenerTodos(incluirBajas);

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
                return ex.ProblemaInterno(logger);
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
                return ex.ProblemaInterno(logger);
            }
        });

        // ── POST /api/productos ────────────────────────────────────────
        group.MapPost("/", async (ProductoCreateDto dto, IProductoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Crear(dto, usuarioActual.CodUsuario());
                if (resultado is null)
                    return Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/productos/{resultado.Cod_Producto}", resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PUT /api/productos/{id} ────────────────────────────────────
        // No recibe/edita el stock (ver ProductoUpdateDto); usar PATCH /{id}/stock para eso.
        group.MapPut("/{id:int}", async (int id, ProductoUpdateDto dto, IProductoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Actualizar(id, dto, usuarioActual.CodUsuario());
                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/productos/{id}/stock ─────────────────────────────
        // Ajuste relativo de stock. Body: { "ajuste": int } (!= 0; positivo = ingreso, negativo =
        // egreso). Reemplaza la edición de Cantidad vía el PUT completo.
        group.MapPatch("/{id:int}/stock", async (int id, AjustarStockDto dto, IProductoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.AjustarStock(id, dto.Ajuste, usuarioActual.CodUsuario());
                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/productos/{id}/reactivar ────────────────────────
        // Reactiva un producto dado de baja.
        group.MapPatch("/{id:int}/reactivar", async (int id, IProductoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Reactivar(id, usuarioActual.CodUsuario());
                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── DELETE /api/productos/{id} ────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, IProductoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id, usuarioActual.CodUsuario());
                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Producto eliminado correctamente", cod_producto = id });
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }
}