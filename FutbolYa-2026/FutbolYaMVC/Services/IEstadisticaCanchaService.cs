using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IEstadisticaCanchaService
{
    Task<EstadisticaCanchaDiaResponse?> ObtenerDia(DateTime fecha, List<int>? canchas);
    Task<EstadisticaCanchaSemanaResponse?> ObtenerSemana(DateTime fecha, List<int>? canchas);
    Task<EstadisticaCanchaMesResponse?> ObtenerMes(DateTime fecha, List<int>? canchas);
}
