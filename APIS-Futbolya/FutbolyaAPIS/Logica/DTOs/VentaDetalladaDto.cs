namespace FutbolyaAPIS.Logica.DTOs;

public record VentaDetalladaDto(
    int Cod_Venta_Detallada,
    int Cod_Venta,
    int Cod_Producto,
    int Cantidad,
    decimal Precio,
    decimal SubTotal
);

public record VentaDetalladaCreateDto(
    int Cod_Venta,
    int Cod_Producto,
    int Cantidad,
    decimal Precio,
    decimal SubTotal
);