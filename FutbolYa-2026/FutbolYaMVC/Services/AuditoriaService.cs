using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/auditoria (historial de alta/modificación/baja).
public class AuditoriaService : IAuditoriaService
{
    private readonly HttpClient _httpClient;

    public AuditoriaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AuditoriaResponse>> GetAllAsync()
    {
        try
        {
            var registros = await _httpClient.GetFromJsonAsync<List<AuditoriaResponse>>("/api/auditoria", JsonDefaults.Options);
            return registros ?? new List<AuditoriaResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener el historial de auditoría: {ex.Message}");
            return new List<AuditoriaResponse>();
        }
    }
}
