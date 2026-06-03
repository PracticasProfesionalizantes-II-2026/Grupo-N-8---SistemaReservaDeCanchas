namespace FutbolyaAPIS.Logica.DTOs;

public record VentaDto(
    int Cod_Venta,
    DateTime Fecha,
    TimeSpan Hora,
    decimal MontoTotal,
    int Cod_Usuario
);

public record VentaCreateDto(
    DateTime Fecha,
    TimeSpan Hora,
    decimal MontoTotal,
    int Cod_Usuario
);