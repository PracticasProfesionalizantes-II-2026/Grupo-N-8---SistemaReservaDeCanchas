using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class CanchaEndpoints
{
    public static void MapCanchaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/canchas").WithTags("Canchas");
        // ── GET /api/canchas ───────────────────────────────────────────
        group.MapGet("/", async (ICanchaLogica logica) =>
        {
            try
            {
                var canchas = await logica.ObtenerTodos();
                return Results.Ok(canchas);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/canchas/{id} ──────────────────────────────────────
        group.MapGet("/{id:int}", async (int id, ICanchaLogica logica) =>
        {
            try
            {
                var cancha = await logica.ObtenerPorId(id);
                if (cancha is null)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/canchas/{id}/disponibilidad ───────────────────────
        group.MapGet("/{id:int}/disponibilidad", async (int id, ICanchaLogica logica) =>
        {
            try
            {
                var cancha = await logica.ObtenerPorId(id);
                if (cancha is null)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                var descripcionEstado = cancha.Estado ? "Disponible" : "En Mantenimiento";
                return Results.Ok(new { cod_cancha = cancha.Cod_Cancha, estado = descripcionEstado });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/canchas ──────────────────────────────────────────
        group.MapPost("/", async (CanchaCreateDto dto, ICanchaLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Descripcion))
                    return Results.BadRequest(new { mensaje = "La descripción es obligatoria" });

                var id = await logica.Crear(dto);
                var creada = await logica.ObtenerPorId(id);

                return Results.Created($"/api/canchas/{id}", creada);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/canchas/{id} ────────────────────────────────────
        group.MapPatch("/{id:int}", async (int id, CanchaDescripcionUpdateDto dto, ICanchaLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Descripcion))
                    return Results.BadRequest(new { mensaje = "La descripción no puede estar vacía" });

                var actualizado = await logica.ActualizarDescripcion(id, dto.Descripcion);
                if (!actualizado)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                var cancha = await logica.ObtenerPorId(id);
                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/canchas/{id}/estado ────────────────────────────
        group.MapPatch("/{id:int}/estado", async (int id, CanchaEstadoUpdateDto dto, ICanchaLogica logica) =>
        {
            try
            {
                var actualizado = await logica.ActualizarEstado(id, dto.Estado);
                if (!actualizado)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                var cancha = await logica.ObtenerPorId(id);
                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/canchas/{id} ───────────────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, ICanchaLogica logica) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id);

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Cancha eliminada correctamente", cod_cancha = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}