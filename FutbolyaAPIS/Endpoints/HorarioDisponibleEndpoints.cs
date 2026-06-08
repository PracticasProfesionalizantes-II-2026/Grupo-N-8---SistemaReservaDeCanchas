using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class HorarioDisponibleEndpoints
{
    public static void MapHorarioDisponibleEndpoints(this IEndpointRouteBuilder app)
    {
        // ── GET /api/horarios ──────────────────────────────────────────
        app.MapGet("/api/horarios", async (IHorarioDisponibleLogica logica) =>
        {
            try
            {
                var horarios = await logica.ObtenerTodos();
                return Results.Ok(horarios);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/horarios/{id} ─────────────────────────────────────
        app.MapGet("/api/horarios/{id:int}", async (int id, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                var horario = await logica.ObtenerPorId(id);
                if (horario is null)
                    return Results.NotFound(new { mensaje = "Horario no encontrado", cod_horario = id });

                return Results.Ok(horario);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/horarios ─────────────────────────────────────────
        app.MapPost("/api/horarios", async (HorarioDisponibleCreateDto dto, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                if (dto.HoraFin <= dto.HoraInicio)
                    return Results.BadRequest(new { mensaje = "La hora de fin debe ser posterior a la hora de inicio" });

                var (resultado, error) = await logica.Crear(dto);

                if (resultado is null)
                    return Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/horarios/{resultado.Cod_Horario}", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PUT /api/horarios/{id} ─────────────────────────────────────
        app.MapPut("/api/horarios/{id:int}", async (int id, HorarioDisponibleCreateDto dto, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                if (dto.HoraFin <= dto.HoraInicio)
                    return Results.BadRequest(new { mensaje = "La hora de fin debe ser posterior a la hora de inicio" });

                var (resultado, error) = await logica.Actualizar(id, dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Horario no encontrado", cod_horario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/horarios/{id}/activo ────────────────────────────
        app.MapPatch("/api/horarios/{id:int}/activo", async (int id, HorarioActivoUpdateDto dto, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                var (resultado, error) = await logica.ActualizarActivo(id, dto.Activo);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Horario no encontrado", cod_horario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { cod_horario = resultado.Cod_Horario, activo = resultado.Activo });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/horarios/{id} ──────────────────────────────────
        app.MapDelete("/api/horarios/{id:int}", async (int id, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id);

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Horario no encontrado", cod_horario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Horario eliminado correctamente", cod_horario = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}