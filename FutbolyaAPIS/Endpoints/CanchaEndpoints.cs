using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class CanchaEndpoints
{
    public static void MapCanchaEndpoints(this IEndpointRouteBuilder app)
    {
        // ── GET /api/canchas ───────────────────────────────────────────
        app.MapGet("/api/canchas", async (ICanchaLogica logica) =>
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
        app.MapGet("/api/canchas/{id:int}", async (int id, ICanchaLogica logica) =>
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
        app.MapGet("/api/canchas/{id:int}/disponibilidad", async (int id, ICanchaLogica logica) =>
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
        app.MapPost("/api/canchas", async (CanchaCreateDto dto, ICanchaLogica logica) =>
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
        app.MapPatch("/api/canchas/{id:int}", async (int id, CanchaDescripcionUpdateDto dto, ICanchaLogica logica) =>
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
        app.MapPatch("/api/canchas/{id:int}/estado", async (int id, CanchaEstadoUpdateDto dto, ICanchaLogica logica) =>
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

        // ── DELETE /api/canchas/{id} ───────────────────────────────────
        app.MapDelete("/api/canchas/{id:int}", async (int id, ICanchaLogica logica) =>
        {
            try
            {
                var cancha = await logica.ObtenerPorId(id);
                if (cancha is null)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                var eliminado = await logica.Eliminar(id);
                if (!eliminado)
                    return Results.Conflict(new { mensaje = "No se puede eliminar una cancha con reservas asociadas" });

                return Results.Ok(new { mensaje = "Cancha eliminada correctamente", cod_cancha = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}