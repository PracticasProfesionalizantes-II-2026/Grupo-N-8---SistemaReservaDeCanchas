using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Seguridad;
using System.Security.Claims;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/horarios: catálogo global de bloques horarios de 1 hora. Capa fina:
// bind DTO → IHorarioDisponibleLogica → status code.
public static class HorarioDisponibleEndpoints
{
    public static void MapHorarioDisponibleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/horarios").WithTags("Horarios Disponibles").RequireAuthorization();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // ── GET /api/horarios ──────────────────────────────────────────
        // Catálogo global. Con ?codCancha= devuelve solo los bloques que esa cancha ofrece.
        group.MapGet("/", async (IHorarioDisponibleLogica logica, int? codCancha) =>
        {
            try
            {
                var horarios = codCancha.HasValue
                    ? await logica.ObtenerPorCancha(codCancha.Value)
                    : await logica.ObtenerTodos();

                return Results.Ok(horarios);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── GET /api/horarios/{id} ─────────────────────────────────────
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
                return ex.ProblemaInterno(logger);
            }
        });

        // ── PATCH /api/horarios/{id}/activo ────────────────────────────
        // "Activo" es la baja lógica del bloque horario: no hay DELETE físico. Solo Administrador
        // y sin acceso desde la UI de MVC — pensado para mantenimiento puntual vía Scalar.
        group.MapPatch("/{id:int}/activo", async (int id, HorarioActivoUpdateDto dto, IHorarioDisponibleLogica logica, ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.ActualizarActivo(id, dto.Activo, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Horario no encontrado", cod_horario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");
    }
}
