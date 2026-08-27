using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class HorarioDisponibleEndpoints
{
    public static void MapHorarioDisponibleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/horarios").WithTags("Horarios Disponibles");

        // ── GET /api/horarios ──────────────────────────────────────────
        //Obtiene todos los horarios disponibles
        group.MapGet("/", async (IHorarioDisponibleLogica logica) =>
        {
            try
            {
                //Obtiene todos los horarios disponibles.
                var horarios = await logica.ObtenerTodos();
                return Results.Ok(horarios);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── GET /api/horarios/{id} ─────────────────────────────────────
        //Obtiene un horario disponible específico por su ID.
        group.MapGet("/{id:int}", async (int id, IHorarioDisponibleLogica logica) =>
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
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── POST /api/horarios ─────────────────────────────────────────
        //Crea un nuevo horario disponible.
        group.MapPost("/", async (HorarioDisponibleCreateDto dto, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                if (dto.HoraFin <= dto.HoraInicio)
                    return Results.BadRequest(new { mensaje = "La hora de fin debe ser posterior a la hora de inicio" });

                //intenta crear un nuevo horario disponible utilizando la lógica de negocio. 
                //devuelve el resultado y un posible mensaje de error.
                var (resultado, error) = await logica.Crear(dto);


                if (resultado is null)
                    // Si no fue posible crear el horario, devuelve el motivo del conflicto.
                    return Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/horarios/{resultado.Cod_Horario}", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PUT /api/horarios/{id} ─────────────────────────────────────
        //Actualiza un horario disponible existente.
        group.MapPut("/{id:int}", async (int id, HorarioDisponibleCreateDto dto, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                if (dto.HoraFin <= dto.HoraInicio)
                    return Results.BadRequest(new { mensaje = "La hora de fin debe ser posterior a la hora de inicio" });

                //intenta actualizar un horario disponible existente utilizando la lógica de negocio.
                var (resultado, error) = await logica.Actualizar(id, dto);

                if (resultado is null)
                    // Si no fue posible actualizar el horario, devuelve el motivo del conflicto.
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
        //Actualiza el estado activo de un horario disponible específico.
        group.MapPatch("/{id:int}/activo", async (int id, HorarioActivoUpdateDto dto, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                // Verifica que el horario exista y actualiza su estado activo utilizando la lógica de negocio.
                var (resultado, error) = await logica.ActualizarActivo(id, dto.Activo);

                if (resultado is null)
                    // Si no fue posible actualizar el estado activo, devuelve el motivo del conflicto.
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
        group.MapDelete("/{id:int}", async (int id, IHorarioDisponibleLogica logica) =>
        {
            try
            {
                // Intenta eliminar un horario disponible utilizando la lógica de negocio.
                var (eliminado, error) = await logica.Eliminar(id);

                if (!eliminado)// Si no fue posible eliminar el horario, devuelve el motivo del conflicto.
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