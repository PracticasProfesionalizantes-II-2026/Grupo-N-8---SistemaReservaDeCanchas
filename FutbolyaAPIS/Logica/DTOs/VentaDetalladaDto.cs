namespace FutbolyaAPIS.Logica.DTOs;

// Lectura — se usará en las respuestas de Venta
public record VentaDetalladaDto(
    int Cod_Venta_Detallada,
    int Cod_Venta,
    int Cod_Producto,
    string Nombre_Producto,
    int Cantidad,
    decimal Precio,
    decimal SubTotal
);

// Escritura — se usará al crear ítems de una venta
public record VentaDetalladaItemDto(
    int Cod_Producto,
    int Cantidad,
    decimal Precio
);