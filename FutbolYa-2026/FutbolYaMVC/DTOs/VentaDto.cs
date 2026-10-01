namespace FutbolYaMVC.DTOs;

// ── Lectura ────────────────────────────────────────────────────────────

public record VentaDetalladaResponse(
    int Cod_Venta_Detallada,
    int Cod_Producto,
    string Nombre_Producto,
    int Cantidad,
    decimal Precio,
    decimal SubTotal
);

public record VentaResponse(
    int Cod_Venta,
    DateTime Fecha,
    TimeSpan Hora,
    decimal MontoTotal,
    int Cod_Usuario,
    string Nombre_Usuario,
    bool Activo,
    List<VentaDetalladaResponse>? Detalle
);

// ── Escritura ──────────────────────────────────────────────────────────

public record VentaDetalladaItemRequest(int Cod_Producto, int Cantidad);

// Lo que llega desde el JS del navegador (sin Cod_Usuario: lo agrega el Controller desde la sesión).
public record VentaCreateRequest(List<VentaDetalladaItemRequest> Detalle);

// Lo que el Service efectivamente envía a la API (con Cod_Usuario ya resuelto).
public record VentaCreateApiRequest(int Cod_Usuario, List<VentaDetalladaItemRequest> Detalle);

// ── ViewModel para la pantalla de listado ────────────────────────────────

public record VentaIndexViewModel(
    List<VentaResponse> Ventas,
    List<ProductoResponse> Productos
);
