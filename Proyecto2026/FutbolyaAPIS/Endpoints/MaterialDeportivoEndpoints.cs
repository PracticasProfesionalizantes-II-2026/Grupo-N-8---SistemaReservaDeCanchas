using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class MaterialDeportivoEndpoints
{
    public static void MapMaterialDeportivoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/materiales").WithTags("Materiales Deportivos");
        // ── GET /api/materiales ────────────────────────────────────────
        //Obtiene todos los materiales deportivos
        group.MapGet("/", async (IMaterialDeportivoLogica logica) =>
        {
            try
            {
                //Obtiene todos los materiales deportivos utilizando la lógica de negocio.
                var materiales = await logica.ObtenerTodos();
                return Results.Ok(materiales);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── GET /api/materiales/{id} ───────────────────────────────────
        //Obtiene un material deportivo específico por su ID.
        group.MapGet("/{id:int}", async (int id, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                //Obtiene un material deportivo específico por su ID utilizando la lógica de negocio.
                var material = await logica.ObtenerPorId(id);
                if (material is null)
                    // Si no se encuentra el material, devuelve un error 404 con un mensaje.
                    return Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id });

                return Results.Ok(material);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── POST /api/materiales ───────────────────────────────────────
        //Crea un nuevo material deportivo.
        group.MapPost("/", async (MaterialDeportivoCreateDto dto, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                // Valida que el nombre no esté vacío.
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return Results.BadRequest(new { mensaje = "El nombre es obligatorio" });

                // Valida que la cantidad no sea negativa.
                if (dto.Cant_Material < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                //intenta crear un nuevo material deportivo utilizando la lógica de negocio.
                var (resultado, error) = await logica.Crear(dto);

                if (resultado is null)
                    // Si no fue posible crear el material, devuelve un error de conflicto con el motivo.
                    return Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/materiales/{resultado.Cod_Material}", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── PUT /api/materiales/{id} ───────────────────────────────────
        //Actualiza un material deportivo existente.
        group.MapPut("/{id:int}", async (int id, MaterialDeportivoCreateDto dto, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                // Valida que el nombre no esté vacío.
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return Results.BadRequest(new { mensaje = "El nombre es obligatorio" });

                // Valida que la cantidad no sea negativa.
                if (dto.Cant_Material < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                //intenta actualizar un material deportivo existente utilizando la lógica de negocio.
                var (resultado, error) = await logica.Actualizar(id, dto);

                if (resultado is null)
                    // Si no fue posible actualizar el material, devuelve un error 404 o 400 según corresponda.
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── PATCH /api/materiales/{id}/stock ──────────────────────────
        //Actualiza la cantidad de un material deportivo existente.
        group.MapPatch("/{id:int}/stock", async (int id, MaterialStockUpdateDto dto, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                // Valida que la cantidad no sea negativa.
                if (dto.Cant_Material < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                //intenta actualizar la cantidad de un material deportivo existente utilizando la lógica de negocio.
                var (resultado, error) = await logica.ActualizarStock(id, dto.Cant_Material);

                if (resultado is null)
                    // Si no fue posible actualizar el material, devuelve un error 404 o 400 según corresponda.
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── DELETE /api/materiales/{id} ────────────────────────────────
        //Elimina un material deportivo existente.
        group.MapDelete("/{id:int}", async (int id, IMaterialDeportivoLogica logica) =>
        {
            try
            {
                //intenta eliminar un material deportivo existente utilizando la lógica de negocio.
                var (eliminado, error) = await logica.Eliminar(id);

                // Si no fue posible eliminar el material, devuelve un error 404 o 409 según corresponda.
                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Material no encontrado", cod_material = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Material eliminado correctamente", cod_material = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });
    }
}