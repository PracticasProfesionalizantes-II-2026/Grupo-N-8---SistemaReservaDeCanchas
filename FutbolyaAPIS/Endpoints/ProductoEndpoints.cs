using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class ProductoEndpoints
{
    public static void MapProductoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/productos").WithTags("Productos");
        // ── GET /api/productos ─────────────────────────────────────────
        // Obtiene todos los productos, permitiendo filtrar por tipo y/o nombre.
        group.MapGet("/", async (
            IProductoLogica logica,
            string? tipo,
            string? nombre) =>
        {
            try
            {
                // Obtiene todos los productos utilizando la lógica de negocio.
                var productos = await logica.ObtenerTodos();

                // Filtra los productos por tipo y/o nombre si se proporcionan parámetros de consulta.
                if (!string.IsNullOrWhiteSpace(tipo))
                    productos = productos.Where(p =>
                        p.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(nombre))
                    productos = productos.Where(p =>
                        p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));

                return Results.Ok(productos);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── GET /api/productos/{id} ────────────────────────────────────
        // Obtiene un producto específico por su ID.
        group.MapGet("/{id:int}", async (int id, IProductoLogica logica) =>
        {
            try
            {
                // Obtiene un producto específico por su ID utilizando la lógica de negocio.
                var producto = await logica.ObtenerPorId(id);
                if (producto is null)
                    // Si no se encuentra el producto, devuelve un error 404 con un mensaje.
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                return Results.Ok(producto);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── POST /api/productos ────────────────────────────────────────
        // Crea un nuevo producto.
        group.MapPost("/", async (ProductoCreateDto dto, IProductoLogica logica) =>
        {
            try
            {
                // Valida que el nombre y el tipo no estén vacíos.
                if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Tipo))
                    return Results.BadRequest(new { mensaje = "Nombre y Tipo son obligatorios" });
                
                var tiposValidos = new[] { "Bebida", "Comida" };
                if (!tiposValidos.Contains(dto.Tipo, StringComparer.OrdinalIgnoreCase))
                    return Results.BadRequest(new { mensaje = "El tipo debe ser 'Bebida' o 'Comida'" });
                
                //intenta crear un nuevo producto utilizando la lógica de negocio.
                var id = await logica.Crear(dto);
                var creado = await logica.ObtenerPorId(id);

                return Results.Created($"/api/productos/{id}", creado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PUT /api/productos/{id} ────────────────────────────────────
        group.MapPut("/{id:int}", async (int id, ProductoCreateDto dto, IProductoLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Tipo))
                    return Results.BadRequest(new { mensaje = "Nombre y Tipo son obligatorios" });

                var tiposValidos = new[] { "Bebida", "Comida" };
                if (!tiposValidos.Contains(dto.Tipo, StringComparer.OrdinalIgnoreCase))
                    return Results.BadRequest(new { mensaje = "El tipo debe ser 'Bebida' o 'Comida'" });
                
                var actualizado = await logica.Actualizar(id, dto);
                if (!actualizado)
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                var producto = await logica.ObtenerPorId(id);
                return Results.Ok(producto);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/productos/{id}/stock ───────────────────────────
        // Actualiza la cantidad de un producto existente.
        group.MapPatch("/{id:int}/stock", async (int id, StockUpdateDto dto, IProductoLogica logica) =>
        {
            try
            {
                // Valida que la cantidad no sea negativa.
                if (dto.Cantidad < 0)
                    return Results.BadRequest(new { mensaje = "La cantidad no puede ser negativa" });

                // Intenta actualizar el stock del producto utilizando la lógica de negocio.
                var actualizado = await logica.ActualizarStock(id, dto.Cantidad);
                if (!actualizado)// Si no se pudo actualizar el stock, devuelve un error 404 con un mensaje.
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                var producto = await logica.ObtenerPorId(id);
                return Results.Ok(producto);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });

        // ── DELETE /api/productos/{id} ────────────────────────────────
        // Elimina un producto existente.
        group.MapDelete("/{id:int}", async (int id, IProductoLogica logica) =>
        {
            try
            {
                // Intenta eliminar un producto existente utilizando la lógica de negocio.
                var producto = await logica.ObtenerPorId(id);
                if (producto is null)
                    return Results.NotFound(new { mensaje = "Producto no encontrado", cod_producto = id });

                var eliminado = await logica.Eliminar(id);
                if (!eliminado)
                    return Results.Conflict(new { mensaje = "No se pudo eliminar el producto, puede tener ventas asociadas" });

                return Results.Ok(new { mensaje = "Producto eliminado correctamente", cod_producto = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
                // Devuelve un error 500 con el mensaje de la excepción.
            }
        });
    }
}