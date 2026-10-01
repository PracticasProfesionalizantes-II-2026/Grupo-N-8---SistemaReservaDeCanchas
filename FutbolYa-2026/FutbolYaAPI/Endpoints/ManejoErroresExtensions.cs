namespace FutbolYaAPI.Endpoints;

// Extensión compartida que el catch de cada endpoint llama como ex.ProblemaInterno(logger):
// loguea la excepción completa server-side y responde un mensaje genérico, para no filtrar
// detalles internos (stack, nombres de tablas, etc.) al cliente.
internal static class ManejoErroresExtensions
{
    internal static IResult ProblemaInterno(this Exception ex, ILogger logger)
    {
        // Conflicto de concurrencia persistente (reintentos agotados) → 409 con mensaje
        // entendible, en el mismo formato { mensaje } que el MVC ya muestra.
        if (ex is FutbolYaAPI.Datos.ConflictoConcurrenciaException)
        {
            logger.LogWarning(ex, "Conflicto de concurrencia persistente tras reintentos");
            return Results.Conflict(new { mensaje = ex.Message });
        }

        logger.LogError(ex, "Error no controlado en un endpoint");
        return Results.Problem("Ocurrió un error interno", statusCode: StatusCodes.Status500InternalServerError);
    }
}
