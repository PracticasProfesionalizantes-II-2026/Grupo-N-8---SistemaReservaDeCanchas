using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/estadisticas (reporte de ventas).
public class EstadisticaService : IEstadisticaService
{
    private readonly HttpClient _httpClient;

    public EstadisticaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static string ArmarQuery(DateTime fecha, List<int>? productos)
    {
        var query = $"?fecha={fecha:yyyy-MM-dd}";
        if (productos != null && productos.Count > 0)
            query += $"&productos={string.Join(",", productos)}";
        return query;
    }

    public async Task<EstadisticaDiaResponse?> ObtenerDia(DateTime fecha, List<int>? productos)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EstadisticaDiaResponse>(
                $"/api/estadisticas/dia{ArmarQuery(fecha, productos)}", JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la estadística del día: {ex.Message}");
            return null;
        }
    }

    public async Task<EstadisticaSemanaResponse?> ObtenerSemana(DateTime fecha, List<int>? productos)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EstadisticaSemanaResponse>(
                $"/api/estadisticas/semana{ArmarQuery(fecha, productos)}", JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la estadística de la semana: {ex.Message}");
            return null;
        }
    }

    public async Task<EstadisticaMesResponse?> ObtenerMes(DateTime fecha, List<int>? productos)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EstadisticaMesResponse>(
                $"/api/estadisticas/mes{ArmarQuery(fecha, productos)}", JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la estadística del mes: {ex.Message}");
            return null;
        }
    }
}
