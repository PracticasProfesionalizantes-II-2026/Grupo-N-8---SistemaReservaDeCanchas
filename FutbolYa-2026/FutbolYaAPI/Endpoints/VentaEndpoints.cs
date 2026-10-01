using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Seguridad;
using System.Security.Claims;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/ventas: alta, consulta, reactivación y anulación de ventas del punto de venta.
// Capa fina: bind DTO → IVentaLogica → status code. Requiere autenticación, sin restricción de rol.
public static class VentaEndpoints
{
    public static void MapVentaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ventas").WithTags("Ventas").RequireAuthorization();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // ── GET /api/ventas ────────────────────────────────────────────
        group.MapGet("/", async (IVentaLogica logica, bool incluirBajas = false) =>
        {
            try
            {
                var ventas = await logica.ObtenerTodos(incluirBajas);
                return Results.Ok(ventas);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── GET /api/ventas/{id} ───────────────────────────────────────
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
                return ex.ProblemaInterno(logger);
            }
        });

        // ── POST /api/ventas ───────────────────────────────────────────
        group.MapPost("/", async (VentaCreateDto dto, IVentaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Crear(dto, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error != null && error.StartsWith("NOT_FOUND")
                        ? Results.NotFound(new { mensaje = SinPrefijoInterno(error) })
                        : Results.BadRequest(new { mensaje = SinPrefijoInterno(error) });

                return Results.Created($"/api/ventas/{resultado.Cod_Venta}", resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/ventas/{id}/reactivar ───────────────────────────
        // Reactiva una venta anulada (vuelve a descontar stock).
        group.MapPatch("/{id:int}/reactivar", async (int id, IVentaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Reactivar(id, usuarioActual.CodUsuario());
                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Venta no encontrada", cod_venta = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── DELETE /api/ventas/{id} ────────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, IVentaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id, usuarioActual.CodUsuario());

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Venta no encontrada", cod_venta = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Venta anulada correctamente", cod_venta = id });
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }

    // La Logica arma algunos errores con un prefijo interno (ej. "INSUFFICIENT_STOCK: ...")
    // que solo sirve para elegir el código de estado acá arriba; el cliente no debe verlo.
    private static string? SinPrefijoInterno(string? error)
    {
        if (error == null) return null;
        var match = System.Text.RegularExpressions.Regex.Match(error, @"^[A-Z_]+:\s*");
        return match.Success ? error[match.Length..] : error;
    }
}
