using FutbolYaAPI.Logica;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/estadisticas: reportes de ventas por día/semana/mes, opcionalmente filtrados
// por producto. Solo Administrador.
public static class EstadisticaEndpoints
{
    public static void MapEstadisticaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/estadisticas").WithTags("Estadisticas").RequireAuthorization("SoloAdministrador");
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // "1,2,3" -> [1,2,3]; vacío/null -> null (= todos los productos)
        // Valida cada id explícitamente para devolver un 400 de validación en vez de dejar
        // que un id no numérico propague una FormatException sin manejar (500).
        static (List<int>? ids, string? error) ParseProductos(string? productos)
        {
            if (string.IsNullOrWhiteSpace(productos))
                return (null, null);

            var partes = productos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ids = new List<int>();
            foreach (var parte in partes)
            {
                if (!int.TryParse(parte, out var id))
                    return (null, $"El id de producto '{parte}' no es válido");
                ids.Add(id);
            }
            return (ids, null);
        }

        // ── GET /api/estadisticas/dia?fecha=&productos= ──────────────────
        group.MapGet("/dia", async (DateTime? fecha, string? productos, IEstadisticaLogica logica) =>
        {
            try
            {
                var (ids, error) = ParseProductos(productos);
                if (error != null)
                    return Results.BadRequest(new { mensaje = error });

                var resultado = await logica.ObtenerVentasDia(fecha ?? DateTime.Today, ids);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── GET /api/estadisticas/semana?fecha=&productos= ───────────────
        group.MapGet("/semana", async (DateTime? fecha, string? productos, IEstadisticaLogica logica) =>
        {
            try
            {
                var (ids, error) = ParseProductos(productos);
                if (error != null)
                    return Results.BadRequest(new { mensaje = error });

                var resultado = await logica.ObtenerVentasSemana(fecha ?? DateTime.Today, ids);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });

        // ── GET /api/estadisticas/mes?fecha=&productos= ──────────────────
        group.MapGet("/mes", async (DateTime? fecha, string? productos, IEstadisticaLogica logica) =>
        {
            try
            {
                var (ids, error) = ParseProductos(productos);
                if (error != null)
                    return Results.BadRequest(new { mensaje = error });

                var resultado = await logica.ObtenerVentasMes(fecha ?? DateTime.Today, ids);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }
}
