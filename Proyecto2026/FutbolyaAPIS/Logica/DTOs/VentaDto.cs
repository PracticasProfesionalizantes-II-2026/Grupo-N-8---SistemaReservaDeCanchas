namespace FutbolyaAPIS.Logica.DTOs;

// ── Lectura ────────────────────────────────────────────────────────────

public record VentaDetalladaDto(
    int Cod_Venta_Detallada,
    int Cod_Producto,
    string Nombre_Producto,
    int Cantidad,
    decimal Precio,
    decimal SubTotal
);

public record VentaDto(
    int Cod_Venta,
    DateTime Fecha,
    TimeSpan Hora,
    decimal MontoTotal,
    int Cod_Usuario,
    string Nombre_Usuario,
    IEnumerable<VentaDetalladaDto>? Detalle = null
);

// ── Escritura ──────────────────────────────────────────────────────────

public record VentaDetalladaItemDto(
    int Cod_Producto,
    int Cantidad
);

public record VentaCreateDto(
    int Cod_Usuario,
    IEnumerable<VentaDetalladaItemDto> Detalle
);