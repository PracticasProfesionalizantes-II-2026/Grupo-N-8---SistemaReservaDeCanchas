namespace FutbolYaAPI.Logica.DTOs;

public record ProductoDto(
    int Cod_Producto,
    string Nombre,
    int Cantidad,
    decimal Precio,
    string Tipo,
    bool Activo
);

public record ProductoCreateDto(
    string Nombre,
    int Cantidad,
    decimal Precio,
    string Tipo
);

// El stock no se edita por PUT (ver AjustarStockDto); el DTO de edición no incluye
// Cantidad para evitar pisar el stock actual con un valor stale del formulario.
public record ProductoUpdateDto(
    string Nombre,
    decimal Precio,
    string Tipo
);