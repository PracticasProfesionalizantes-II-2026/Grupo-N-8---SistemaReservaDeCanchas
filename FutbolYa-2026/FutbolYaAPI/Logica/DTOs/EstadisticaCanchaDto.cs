namespace FutbolYaAPI.Logica.DTOs;

// Fila genérica reusada por los 3 reportes: una cancha + una serie de valores
// (horarios en el reporte Día, días en Semana/Mes) + su total.
public record FilaCanchaDto(int Cod_Cancha, string Cancha, List<int> Valores, int Total);

public record EstadisticaCanchaDiaDto(
    DateTime Fecha,
    List<string> Horarios,
    List<FilaCanchaDto> Filas,
    int TotalReservas);

public record EstadisticaCanchaSemanaDto(
    DateTime Desde,
    DateTime Hasta,
    List<string> Dias,
    List<FilaCanchaDto> Filas,
    int TotalReservas);

public record EstadisticaCanchaMesDto(
    DateTime Desde,
    DateTime Hasta,
    List<int> Dias,
    List<FilaCanchaDto> Filas,
    int TotalReservas);
