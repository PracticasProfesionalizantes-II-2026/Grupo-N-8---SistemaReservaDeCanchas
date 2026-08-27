using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class CanchaEndpoints
{
    public static void MapCanchaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/canchas").WithTags("Canchas");
        // ── GET /api/canchas ───────────────────────────────────────────
        //Obtiene la lista de todas las canchas registradas.
        group.MapGet("/", async (ICanchaLogica logica) =>
        {
            try
            {
                //Obtiene todas las canchas.
                var canchas = await logica.ObtenerTodos();
                return Results.Ok(canchas);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/canchas/{id} ──────────────────────────────────────
        //Obtiene la información de una cancha específica por su ID.
        group.MapGet("/{id:int}", async (int id, ICanchaLogica logica) =>
        {
            try
            {
                //buscamos la cancha por su id
                var cancha = await logica.ObtenerPorId(id);
                if (cancha is null)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/canchas/{id}/disponibilidad ───────────────────────
        //Obtiene la disponibilidad de una cancha específica por su ID.
        group.MapGet("/{id:int}/disponibilidad", async (int id, ICanchaLogica logica) =>
        {
            try
            {
                //buscamos la cancha por su id
                var cancha = await logica.ObtenerPorId(id);
                if (cancha is null)
                    // Si la cancha no existe, devuelve un error 404.
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });
                // Convierte el valor booleano del estado en una descripción legible.
                var descripcionEstado = cancha.Estado ? "Disponible" : "En Mantenimiento";
                // Devuelve el código de la cancha y su estado de disponibilidad.
                return Results.Ok(new { cod_cancha = cancha.Cod_Cancha, estado = descripcionEstado });
            }
            // Captura cualquier error inesperado durante la consulta.
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/canchas ──────────────────────────────────────────
        //Crea una nueva cancha.
        group.MapPost("/", async (CanchaCreateDto dto, ICanchaLogica logica) =>
        {
            try
            {
                // Validamos que la descripción no esté vacía.
                if (string.IsNullOrWhiteSpace(dto.Descripcion))
                    return Results.BadRequest(new { mensaje = "La descripción es obligatoria" });

                // Creamos la nueva cancha y obtenemos su ID. En canchaLogica, el nombre se asigna automáticamente como "Cancha N° {id}".
                var id = await logica.Crear(dto);
                var creada = await logica.ObtenerPorId(id);

                return Results.Created($"/api/canchas/{id}", creada);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/canchas/{id} ────────────────────────────────────
        //Actualiza la descripción de una cancha específica por su ID.
        group.MapPatch("/{id:int}", async (int id, CanchaDescripcionUpdateDto dto, ICanchaLogica logica) =>
        {
            try
            {
                // Validamos que la descripción no esté vacía.
                if (string.IsNullOrWhiteSpace(dto.Descripcion))
                    return Results.BadRequest(new { mensaje = "La descripción no puede estar vacía" });
                
                //Actualizamos la descripción de la cancha. Si no se encuentra, devolvemos un error 404.
                var actualizado = await logica.ActualizarDescripcion(id, dto.Descripcion);
                if (!actualizado)
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });
                //si se actualizó correctamente, obtenemos la cancha actualizada y la devolvemos en la respuesta.
                var cancha = await logica.ObtenerPorId(id);
                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/canchas/{id}/estado ────────────────────────────
        //Actualiza el estado de una cancha específica por su ID.
        group.MapPatch("/{id:int}/estado", async (int id, CanchaEstadoUpdateDto dto, ICanchaLogica logica) =>
        {
            try
            {
                // Intenta actualizar el estado de la cancha. Se recibe un valor booleano que indica si la cancha está disponible (true) o en mantenimiento (false).
                var actualizado = await logica.ActualizarEstado(id, dto.Estado);
                if (!actualizado)
                    // Si no se encuentra la cancha, devuelve un error 404.
                    return Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id });

                // Si se actualizó correctamente, obtenemos la cancha actualizada y la devolvemos en la respuesta.
                var cancha = await logica.ObtenerPorId(id);
                return Results.Ok(cancha);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/canchas/{id} ───────────────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, ICanchaLogica logica) =>
        {
            try
            {
                // Intenta eliminar la cancha y obtiene el resultado de la operación 
                // junto con un posible mensaje de error.
                var (eliminado, error) = await logica.Eliminar(id);

                if (!eliminado)
                    // Si no fue posible eliminar la cancha, devuelve el error correspondiente.
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Cancha no encontrada", cod_cancha = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Cancha eliminada correctamente", cod_cancha = id });
                // Confirma que la cancha fue eliminada correctamente.
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}