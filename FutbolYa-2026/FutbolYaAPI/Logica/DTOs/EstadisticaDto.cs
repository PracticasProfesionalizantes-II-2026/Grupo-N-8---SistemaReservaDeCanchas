namespace FutbolYaAPI.Logica.DTOs;

// ── Reporte "Día" (pie chart + tabla por producto) ──────────────────────

public record ProductoVendidoDto(int Cod_Producto, string Nombre, int Cantidad, decimal Total);

public record EstadisticaDiaDto(
    DateTime Fecha,
    List<ProductoVendidoDto> Productos,
    int TotalCantidad,
    decimal TotalMonto);

// ── Reporte "Semana" / "Mes" (bar chart + tabla por punto) ──────────────

public record PuntoDto(string Etiqueta, int Cantidad, decimal Total);

public record EstadisticaSemanaDto(
    DateTime Desde,
    DateTime Hasta,
    List<PuntoDto> Dias,
    int TotalCantidad,
    decimal TotalMonto);

public record EstadisticaMesDto(
    DateTime Desde,
    DateTime Hasta,
    List<PuntoDto> Semanas,
    int TotalCantidad,
    decimal TotalMonto);
