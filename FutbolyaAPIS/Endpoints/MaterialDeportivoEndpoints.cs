using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class MaterialDeportivoEndpoints
{
    public static void MapMaterialDeportivoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/materiales").WithTags("Materiales Deportivos");
        // ── GET /api/materiales ────────────────────────────────────────
        group.MapGet("/", async (IMaterialDeportivoLogica logica) =>
        {
            try
            {
                var materiales = await logica.ObtenerTodos();
                return Results.Ok(materiales);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
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
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/materiales ───────────────────────────────────────
        group.MapPost("/", async (MaterialDeportivoCreateDto dto, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return Results.BadRequest(new { mensaje = "El nombre es obligatorio" });

                if (dto.Cant_Material < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                var (resultado, error) = await logica.Crear(dto);

                if (resultado is null)
                    return Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/materiales/{resultado.Cod_Material}", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PUT /api/materiales/{id} ───────────────────────────────────
        group.MapPut("/{id:int}", async (int id, MaterialDeportivoCreateDto dto, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return Results.BadRequest(new { mensaje = "El nombre es obligatorio" });

                if (dto.Cant_Material < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                var (resultado, error) = await logica.Actualizar(id, dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/materiales/{id}/stock ──────────────────────────
        group.MapPatch("/{id:int}/stock", async (int id, MaterialStockUpdateDto dto, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                if (dto.Cant_Material < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                var (resultado, error) = await logica.ActualizarStock(id, dto.Cant_Material);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/materiales/{id} ────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id);

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Material eliminado correctamente", cod_material = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}