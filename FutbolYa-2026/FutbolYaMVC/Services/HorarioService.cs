using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/horarios (catálogo global de bloques horarios).
public class HorarioService : IHorarioService
{
    private readonly HttpClient _httpClient;

    public HorarioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<HorarioResponse>> GetAllAsync()
    {
        try
        {
            var horarios = await _httpClient.GetFromJsonAsync<List<HorarioResponse>>("/api/horarios", JsonDefaults.Options);
            return (horarios ?? new List<HorarioResponse>())
                .OrderBy(h => h.HoraInicio)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener horarios: {ex.Message}");
            return new List<HorarioResponse>();
        }
    }
}
