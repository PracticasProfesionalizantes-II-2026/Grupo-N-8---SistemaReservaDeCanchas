using FutbolYaAPI.Logica;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/auditoria: consulta de solo lectura sobre el historial de alta/modificación/baja
// que registran las demás Logica. Capa fina: delega en IAuditoriaLogica y solo Administrador
// puede consultarlo.
public static class AuditoriaEndpoints
{
    public static void MapAuditoriaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auditoria").WithTags("Auditoría").RequireAuthorization("SoloAdministrador");
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        group.MapGet("/", async (IAuditoriaLogica logica, string? entidad, int? codUsuario) =>
        {
            try
            {
                var registros = await logica.ObtenerTodos(entidad, codUsuario);
                return Results.Ok(registros);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        });
    }
}
