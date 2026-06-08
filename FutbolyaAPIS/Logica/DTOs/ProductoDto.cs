namespace FutbolyaAPIS.Logica.DTOs;

public record ProductoDto(
    int Cod_Producto,
    string Nombre,
    int Cantidad,
    decimal Precio,
    string Tipo
);

public record ProductoCreateDto(
    string Nombre,
    int Cantidad,
    decimal Precio,
    string Tipo
);

public record StockUpdateDto(int Cantidad);