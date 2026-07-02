using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class VentaEndpoints
{
    public static void MapVentaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ventas").WithTags("Ventas");
        // ── GET /api/ventas ────────────────────────────────────────────
        //Obtiene todas las ventas registradas en la base de datos.
        group.MapGet("/", async (IVentaLogica logica) =>
        {
            try
            {
                var ventas = await logica.ObtenerTodos();
                return Results.Ok(ventas);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/ventas/{id} ───────────────────────────────────────
        //Obtiene una venta específica por su ID.
        group.MapGet("/{id:int}", async (int id, IVentaLogica logica) =>
        {
            try
            {
                var venta = await logica.ObtenerPorId(id);
                if (venta is null)
                    return Results.NotFound(new { mensaje = "Venta no encontrada", cod_venta = id });

                return Results.Ok(venta);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/ventas ───────────────────────────────────────────
        //Crea una nueva venta.
        group.MapPost("/", async (VentaCreateDto dto, IVentaLogica logica) =>
        {
            try
            {
                // Validaciones básicas
                if (dto.Detalle == null || !dto.Detalle.Any())
                    return Results.BadRequest(new { mensaje = "La venta debe tener al menos un producto en el detalle" });

                if (dto.Detalle.Any(d => d.Cantidad <= 0))
                    return Results.BadRequest(new { mensaje = "La cantidad de cada producto debe ser mayor a 0" });

                // Llamada a la lógica de negocio para crear la venta
                var (resultado, error) = await logica.Crear(dto);

                // Si el resultado es nulo, significa que hubo un error en la creación de la venta
                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = error })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Created($"/api/ventas/{resultado.Cod_Venta}", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/ventas/{id} ────────────────────────────────────
        //Elimina una venta específica por su ID.
        group.MapDelete("/{id:int}", async (int id, IVentaLogica logica) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id);

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Venta no encontrada", cod_venta = id })
                        : Results.Problem(error);

                return Results.Ok(new { mensaje = "Venta anulada correctamente", cod_venta = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}