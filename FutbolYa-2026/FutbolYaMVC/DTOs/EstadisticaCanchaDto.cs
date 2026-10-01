namespace FutbolYaMVC.DTOs;

public record FilaCanchaResponse(int Cod_Cancha, string Cancha, List<int> Valores, int Total);

public record EstadisticaCanchaDiaResponse(
    DateTime Fecha,
    List<string> Horarios,
    List<FilaCanchaResponse> Filas,
    int TotalReservas
);

public record EstadisticaCanchaSemanaResponse(
    DateTime Desde,
    DateTime Hasta,
    List<string> Dias,
    List<FilaCanchaResponse> Filas,
    int TotalReservas
);

public record EstadisticaCanchaMesResponse(
    DateTime Desde,
    DateTime Hasta,
    List<int> Dias,
    List<FilaCanchaResponse> Filas,
    int TotalReservas
);
