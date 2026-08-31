using FutbolyaMVC.DTOs;
using System.Text.Json;

namespace FutbolyaMVC.Services;

public class CanchaService : ICanchaService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5007";

    public CanchaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CanchaResponse>> GetAllAsync()
    {
        try
        {
            var canchas = await _httpClient.GetFromJsonAsync<List<CanchaResponse>>(
                $"{_apiBaseUrl}/api/canchas");

            return canchas ?? new List<CanchaResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener canchas: {ex.Message}");
            return new List<CanchaResponse>();
        }
    }

    public async Task<CanchaResponse?> CreateAsync(CanchaCreateRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/canchas", request);

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<CanchaResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear cancha: {ex.Message}");
            return null;
        }
    }

    public async Task<CanchaResponse?> UpdateAsync(int id, CanchaUpdateRequest request)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"{_apiBaseUrl}/api/canchas/{id}", request);

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<CanchaResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al editar cancha: {ex.Message}");
            return null;
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"{_apiBaseUrl}/api/canchas/{id}");

            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

            // 409: tiene reservas asociadas / 404: no existe. En ambos casos
            // la API devuelve { "mensaje": "..." } en el body.
            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var mensaje = jsonDoc.RootElement.TryGetProperty("mensaje", out var m)
                ? m.GetString()
                : "No se pudo eliminar la cancha";

            return new DeleteResult(false, mensaje);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar cancha: {ex.Message}");
            return new DeleteResult(false, "Error de conexión con el servidor");
        }
    }
}