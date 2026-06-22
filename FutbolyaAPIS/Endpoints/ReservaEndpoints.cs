using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class ReservaEndpoints
{
    public static void MapReservaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservas").WithTags("Reservas");
        // ── GET /api/reservas ──────────────────────────────────────────
        group.MapGet("/", async (IReservaLogica logica) =>
        {
            try
            {
                var reservas = await logica.ObtenerTodos();
                return Results.Ok(reservas);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/reservas/{id} ─────────────────────────────────────
        group.MapGet("/{id:int}", async (int id, IReservaLogica logica) =>
        {
            try
            {
                var reserva = await logica.ObtenerPorId(id);
                if (reserva is null)
                    return Results.NotFound(new { mensaje = "Reserva no encontrada", cod_reserva = id });

                return Results.Ok(reserva);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/reservas ─────────────────────────────────────────
        group.MapPost("/", async (ReservaCreateDto dto, IReservaLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Dni_Cliente) || string.IsNullOrWhiteSpace(dto.Telefono_Cliente))
                    return Results.BadRequest(new { mensaje = "DNI y teléfono del cliente son obligatorios" });

                var (resultado, error) = await logica.Crear(dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Cancha, usuario, horario o material no encontrado" })
                        : Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/reservas/{resultado.Cod_Reserva}", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PUT /api/reservas/{id} ─────────────────────────────────────
        group.MapPut("/{id:int}", async (int id, ReservaUpdateDto dto, IReservaLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Dni_Cliente) || string.IsNullOrWhiteSpace(dto.Telefono_Cliente))
                    return Results.BadRequest(new { mensaje = "DNI y teléfono del cliente son obligatorios" });

                var (resultado, error) = await logica.Actualizar(id, dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Reserva, cancha, horario o material no encontrado" })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/reservas/{id} ──────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, IReservaLogica logica) =>
        {
            try
            {
                var reserva = await logica.ObtenerPorId(id);
                if (reserva is null)
                    return Results.NotFound(new { mensaje = "Reserva no encontrada", cod_reserva = id });

                await logica.Eliminar(id);
                return Results.Ok(new { mensaje = "Reserva cancelada correctamente", cod_reserva = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/reservas/{id}/materiales ──────────────────────────
        group.MapGet("/{id:int}/materiales", async (int id, IReservaLogica logica) =>
        {
            try
            {
                var reserva = await logica.ObtenerPorId(id);
                if (reserva is null)
                    return Results.NotFound(new { mensaje = "Reserva no encontrada", cod_reserva = id });

                var materiales = await logica.ObtenerMateriales(id);
                return Results.Ok(materiales);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/reservas/{id}/materiales ─────────────────────────
        group.MapPost("/{id:int}/materiales", async (int id, ReservaMaterialAddDto dto, IReservaLogica logica) =>
        {
            try
            {
                if (dto.Cantidad <= 0)
                    return Results.BadRequest(new { mensaje = "La cantidad debe ser mayor a 0" });

                var (resultado, error) = await logica.AgregarMaterial(id, dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Reserva o material no encontrado" })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Created($"/api/reservas/{id}/materiales", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/reservas/{id}/materiales/{mat_id} ─────────────
        group.MapDelete("/{id:int}/materiales/{mat_id:int}", async (int id, int mat_id, IReservaLogica logica) =>
        {
            try
            {
                var (eliminado, error) = await logica.QuitarMaterial(id, mat_id);

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Reserva o material no encontrado" })
                        : Results.Problem(error);

                return Results.Ok(new { mensaje = "Material eliminado de la reserva correctamente", cod_reserva_mat = mat_id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}