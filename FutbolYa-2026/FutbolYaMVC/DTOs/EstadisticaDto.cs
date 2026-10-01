namespace FutbolYaMVC.DTOs;

// ── Reporte Día ────────────────────────────────────────────────────────

public record ProductoVendidoResponse(int Cod_Producto, string Nombre, int Cantidad, decimal Total);

public record EstadisticaDiaResponse(
    DateTime Fecha,
    List<ProductoVendidoResponse> Productos,
    int TotalCantidad,
    decimal TotalMonto
);

// ── Reporte Semana / Mes ──────────────────────────────────────────────

public record PuntoResponse(string Etiqueta, int Cantidad, decimal Total);

public record EstadisticaSemanaResponse(
    DateTime Desde,
    DateTime Hasta,
    List<PuntoResponse> Dias,
    int TotalCantidad,
    decimal TotalMonto
);

public record EstadisticaMesResponse(
    DateTime Desde,
    DateTime Hasta,
    List<PuntoResponse> Semanas,
    int TotalCantidad,
    decimal TotalMonto
);

// ── ViewModel de la pantalla principal ─────────────────────────────

public record EstadisticaIndexViewModel(
    List<ProductoResponse> Productos,
    List<CanchaResponse> Canchas
);
