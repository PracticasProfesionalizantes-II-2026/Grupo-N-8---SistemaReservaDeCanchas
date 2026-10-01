using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;
using System.Text.Json;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/canchas; mismo patrón que el resto de los Services.
public class CanchaService : ICanchaService
{
    private readonly HttpClient _httpClient;

    public CanchaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CanchaResponse>> GetAllAsync()
    {
        try
        {
            var canchas = await _httpClient.GetFromJsonAsync<List<CanchaResponse>>("/api/canchas?incluirBajas=true", JsonDefaults.Options);
            return canchas ?? new List<CanchaResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener canchas: {ex.Message}");
            return new List<CanchaResponse>();
        }
    }

    public async Task<(CanchaResponse? cancha, string? error)> CreateAsync(CanchaCreateRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/canchas", request, JsonDefaults.Options);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<CanchaResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            return (null, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    public async Task<CanchaResponse?> UpdateAsync(int id, CanchaUpdateRequest request)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/canchas/{id}", request, JsonDefaults.Options);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<CanchaResponse>(JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al editar cancha: {ex.Message}");
            return null;
        }
    }

    // Cambio de estado (Disponible / Mantenimiento / Baja).
    public async Task<(CanchaResponse? cancha, string? error)> ActualizarEstadoAsync(int id, string estado)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/canchas/{id}/estado", new CanchaEstadoUpdateRequest(estado), JsonDefaults.Options);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<CanchaResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            return (null, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    // Qué bloques del catálogo global de horarios ofrece esta cancha.
    public async Task<(CanchaResponse? cancha, string? error)> ActualizarHorariosAsync(int id, List<int> codHorarios)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/canchas/{id}/horarios", new CanchaHorariosUpdateRequest(codHorarios), JsonDefaults.Options);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<CanchaResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            return (null, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/canchas/{id}");
            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

            return new DeleteResult(false, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar cancha: {ex.Message}");
            return new DeleteResult(false, "Error de conexión con el servidor");
        }
    }

    private static async Task<string> LeerMensajeError(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            return jsonDoc.RootElement.TryGetProperty("mensaje", out var m)
                ? (m.GetString() ?? "Ocurrió un error")
                : "Ocurrió un error";
        }
        catch
        {
            return "Ocurrió un error";
        }
    }
}
