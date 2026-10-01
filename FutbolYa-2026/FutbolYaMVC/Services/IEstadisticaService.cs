using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IEstadisticaService
{
    Task<EstadisticaDiaResponse?> ObtenerDia(DateTime fecha, List<int>? productos);
    Task<EstadisticaSemanaResponse?> ObtenerSemana(DateTime fecha, List<int>? productos);
    Task<EstadisticaMesResponse?> ObtenerMes(DateTime fecha, List<int>? productos);
}
