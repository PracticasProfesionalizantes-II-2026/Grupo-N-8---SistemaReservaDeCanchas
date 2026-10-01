using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Seguridad;
using System.Security.Claims;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/canchas: alta, consulta, edición de estado/horarios y baja de canchas. Capa fina:
// bind DTO → ICanchaLogica → status code. Requiere autenticación, sin restricción de rol.
public static class CanchaEndpoints
{
    public static void MapCanchaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/canchas").WithTags("Canchas").RequireAuthorization();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // ── GET /api/canchas ───────────────────────────────────────────
        group.MapGet("/", async (ICanchaLogica logica, bool incluirBajas = false) =>
        {
            try
            {
                var canchas = await logica.ObtenerTodos(incluirBajas);
                return Results.Ok(canchas);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
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
                return ex.ProblemaInterno(logger);
            }
        });

        // ── POST /api/canchas ──────────────────────────────────────────
        group.MapPost("/", async (CanchaCreateDto dto, ICanchaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Descripcion))
                    return Results.BadRequest(new { mensaje = "La descripción es obligatoria" });

                var (id, error) = await logica.Crear(dto, usuarioActual.CodUsuario());
                if (id is null)
                    return Results.Conflict(new { mensaje = error });

                var creada = await logica.ObtenerPorId(id.Value);
                return Results.Created($"/api/canchas/{id}", creada);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/canchas/{id} ────────────────────────────────────
        group.MapPatch("/{id:int}", async (int id, CanchaDescripcionUpdateDto dto, ICanchaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Descripcion))
                    return Results.BadRequest(new { mensaje = "La descripción no puede estar vacía" });

                var actualizado = await logica.ActualizarDescripcion(id, dto.Descripcion, usuarioActual.CodUsuario());
                if (!actualizado)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                var cancha = await logica.ObtenerPorId(id);
                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/canchas/{id}/estado ─────────────────────────────
        group.MapPatch("/{id:int}/estado", async (int id, CanchaEstadoUpdateDto dto, ICanchaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (actualizado, error) = await logica.ActualizarEstado(id, dto.Estado, usuarioActual.CodUsuario());
                if (!actualizado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id })
                        : Results.Conflict(new { mensaje = error });

                var cancha = await logica.ObtenerPorId(id);
                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PUT /api/canchas/{id}/horarios ─────────────────────────────
        // Qué bloques del catálogo global de horarios ofrece esta cancha.
        group.MapPut("/{id:int}/horarios", async (int id, CanchaHorariosUpdateDto dto, ICanchaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (actualizado, error) = await logica.ActualizarHorarios(id, dto.Cod_Horarios, usuarioActual.CodUsuario());
                if (!actualizado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id })
                        : Results.Conflict(new { mensaje = error });

                var cancha = await logica.ObtenerPorId(id);
                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── DELETE /api/canchas/{id} ────────────────────────────────────────
        // Baja lógica; bloqueada por Logica si hay reservas pendientes sobre esta cancha.
        group.MapDelete("/{id:int}", async (int id, ICanchaLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id, usuarioActual.CodUsuario());

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Cancha eliminada correctamente", cod_cancha = id });
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }
}
