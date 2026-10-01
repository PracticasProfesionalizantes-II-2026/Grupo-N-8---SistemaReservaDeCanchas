using FutbolYaAPI.Logica;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/estadisticas-cancha: reportes de reservas por día/semana/mes, opcionalmente
// filtrados por cancha. Solo Administrador.
public static class EstadisticaCanchaEndpoints
{
    public static void MapEstadisticaCanchaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/estadisticas-cancha").WithTags("EstadisticasCancha").RequireAuthorization("SoloAdministrador");
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Valida cada id explícitamente para devolver un 400 de validación en vez de dejar
        // que un id no numérico propague una FormatException sin manejar (500).
        static (List<int>? ids, string? error) ParseCanchas(string? canchas)
        {
            if (string.IsNullOrWhiteSpace(canchas))
                return (null, null);

            var partes = canchas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ids = new List<int>();
            foreach (var parte in partes)
            {
                if (!int.TryParse(parte, out var id))
                    return (null, $"El id de cancha '{parte}' no es válido");
                ids.Add(id);
            }
            return (ids, null);
        }

        // ── GET /api/estadisticas-cancha/dia?fecha=&canchas= ─────────────
        group.MapGet("/dia", async (DateTime? fecha, string? canchas, IEstadisticaCanchaLogica logica) =>
        {
            try
            {
                var (ids, error) = ParseCanchas(canchas);
                if (error != null)
                    return Results.BadRequest(new { mensaje = error });

                var resultado = await logica.ObtenerReservasDia(fecha ?? DateTime.Today, ids);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── GET /api/estadisticas-cancha/semana?fecha=&canchas= ──────────
        group.MapGet("/semana", async (DateTime? fecha, string? canchas, IEstadisticaCanchaLogica logica) =>
        {
            try
            {
                var (ids, error) = ParseCanchas(canchas);
                if (error != null)
                    return Results.BadRequest(new { mensaje = error });

                var resultado = await logica.ObtenerReservasSemana(fecha ?? DateTime.Today, ids);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── GET /api/estadisticas-cancha/mes?fecha=&canchas= ─────────────
        group.MapGet("/mes", async (DateTime? fecha, string? canchas, IEstadisticaCanchaLogica logica) =>
        {
            try
            {
                var (ids, error) = ParseCanchas(canchas);
                if (error != null)
                    return Results.BadRequest(new { mensaje = error });

                var resultado = await logica.ObtenerReservasMes(fecha ?? DateTime.Today, ids);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }
}
