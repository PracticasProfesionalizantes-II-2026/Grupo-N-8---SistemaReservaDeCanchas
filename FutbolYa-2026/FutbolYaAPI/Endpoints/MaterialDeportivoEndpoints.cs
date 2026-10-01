using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Seguridad;
using System.Security.Claims;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/materiales: catálogo de material deportivo en alquiler. Capa fina:
// bind DTO → IMaterialDeportivoLogica → status code. El stock ya no se edita vía PUT (ver más
// abajo); solo se mueve con PATCH /{id}/stock, que es atómico y auditado.
public static class MaterialDeportivoEndpoints
{
    public static void MapMaterialDeportivoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/materiales").WithTags("Materiales Deportivos").RequireAuthorization();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();
        // ── GET /api/materiales ────────────────────────────────────────
        group.MapGet("/", async (IMaterialDeportivoLogica logica, bool incluirBajas = false) =>
        {
            try
            {
                var materiales = await logica.ObtenerTodos(incluirBajas);
                return Results.Ok(materiales);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── GET /api/materiales/{id} ───────────────────────────────────
        group.MapGet("/{id:int}", async (int id, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                var material = await logica.ObtenerPorId(id);
                if (material is null)
                    return Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id });

                return Results.Ok(material);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── POST /api/materiales ───────────────────────────────────────
        group.MapPost("/", async (MaterialDeportivoCreateDto dto, IMaterialDeportivoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Crear(dto, usuarioActual.CodUsuario());

                if (resultado is null)
                    return Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/materiales/{resultado.Cod_Material}", resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PUT /api/materiales/{id} ───────────────────────────────────
        // No recibe/edita el stock (ver MaterialDeportivoUpdateDto); usar PATCH /{id}/stock
        // para eso.
        group.MapPut("/{id:int}", async (int id, MaterialDeportivoUpdateDto dto, IMaterialDeportivoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Actualizar(id, dto, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/materiales/{id}/stock ────────────────────────────
        // Ajuste relativo de stock. Body: { "ajuste": int } (!= 0; positivo = ingreso, negativo =
        // egreso). Reemplaza la edición de Cant_Material vía el PUT completo.
        group.MapPatch("/{id:int}/stock", async (int id, AjustarStockDto dto, IMaterialDeportivoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.AjustarStock(id, dto.Ajuste, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/materiales/{id}/reactivar ───────────────────────
        // Reactiva un material dado de baja.
        group.MapPatch("/{id:int}/reactivar", async (int id, IMaterialDeportivoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Reactivar(id, usuarioActual.CodUsuario());
                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── DELETE /api/materiales/{id} ────────────────────────────────
        // Baja lógica; bloqueada por Logica si hay reservas pendientes que usan el material.
        group.MapDelete("/{id:int}", async (int id, IMaterialDeportivoLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id, usuarioActual.CodUsuario());

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Material eliminado correctamente", cod_material = id });
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }
}