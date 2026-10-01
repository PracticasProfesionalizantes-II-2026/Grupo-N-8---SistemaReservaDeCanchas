using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/estadisticas-cancha (reporte de reservas por cancha).
public class EstadisticaCanchaService : IEstadisticaCanchaService
{
    private readonly HttpClient _httpClient;

    public EstadisticaCanchaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static string ArmarQuery(DateTime fecha, List<int>? canchas)
    {
        var query = $"?fecha={fecha:yyyy-MM-dd}";
        if (canchas != null && canchas.Count > 0)
            query += $"&canchas={string.Join(",", canchas)}";
        return query;
    }

    public async Task<EstadisticaCanchaDiaResponse?> ObtenerDia(DateTime fecha, List<int>? canchas)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EstadisticaCanchaDiaResponse>(
                $"/api/estadisticas-cancha/dia{ArmarQuery(fecha, canchas)}", JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la estadística de canchas del día: {ex.Message}");
            return null;
        }
    }

    public async Task<EstadisticaCanchaSemanaResponse?> ObtenerSemana(DateTime fecha, List<int>? canchas)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EstadisticaCanchaSemanaResponse>(
                $"/api/estadisticas-cancha/semana{ArmarQuery(fecha, canchas)}", JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la estadística de canchas de la semana: {ex.Message}");
            return null;
        }
    }

    public async Task<EstadisticaCanchaMesResponse?> ObtenerMes(DateTime fecha, List<int>? canchas)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EstadisticaCanchaMesResponse>(
                $"/api/estadisticas-cancha/mes{ArmarQuery(fecha, canchas)}", JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la estadística de canchas del mes: {ex.Message}");
            return null;
        }
    }
}
