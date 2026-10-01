using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Seguridad;
using System.Security.Claims;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/reservas: alta, consulta y cancelación de reservas de cancha. Capa fina:
// bind DTO → IReservaLogica → status code. Requiere autenticación, sin restricción de rol.
public static class ReservaEndpoints
{
    public static void MapReservaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservas").WithTags("Reservas").RequireAuthorization();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

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
                return ex.ProblemaInterno(logger);
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
                return ex.ProblemaInterno(logger);
            }
        });

        // ── POST /api/reservas ─────────────────────────────────────────
        // dto.Cod_Horarios es una lista de uno o más bloques contiguos de 1 hora.
        group.MapPost("/", async (ReservaCreateDto dto, IReservaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Dni_Cliente) || string.IsNullOrWhiteSpace(dto.Telefono_Cliente))
                    return Results.BadRequest(new { mensaje = "DNI y teléfono del cliente son obligatorios" });

                var (resultado, error) = await logica.Crear(dto, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error != null && error.Contains("no encontrad")
                        ? Results.NotFound(new { mensaje = error })
                        : Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/reservas/{resultado.Cod_Reserva}", resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── DELETE /api/reservas/{id} ──────────────────────────────────
        // Cancela (baja lógica vía Estado), no elimina físicamente — se conserva el historial.
        group.MapDelete("/{id:int}", async (int id, IReservaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (cancelada, error) = await logica.Cancelar(id, usuarioActual.CodUsuario());
                if (!cancelada)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Reserva no encontrada", cod_reserva = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Reserva cancelada correctamente", cod_reserva = id });
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }
}
