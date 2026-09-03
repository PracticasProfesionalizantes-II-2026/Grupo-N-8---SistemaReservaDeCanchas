using FutbolyaMVC.DTOs;
using System.Text.Json;

namespace FutbolyaMVC.Services;

public class MaterialService : IMaterialService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5007";

    public MaterialService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MaterialResponse>> GetAllAsync()
    {
        try
        {
            var materiales = await _httpClient.GetFromJsonAsync<List<MaterialResponse>>(
                $"{_apiBaseUrl}/api/materiales");

            return materiales ?? new List<MaterialResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener materiales: {ex.Message}");
            return new List<MaterialResponse>();
        }
    }

    public async Task<(MaterialResponse? material, string? error)> CreateAsync(MaterialCreateRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/materiales", request);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<MaterialResponse>(), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear material: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<(MaterialResponse? material, string? error)> UpdateAsync(int id, MaterialCreateRequest request)
    {
        try
        {
            // La API expone PUT (reemplazo completo) para materiales, igual que Productos.
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/materiales/{id}", request);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<MaterialResponse>(), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al editar material: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/materiales/{id}");

            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

            // 404: no existe / 409: tiene reservas asociadas. La API devuelve { "mensaje": "..." }.
            return new DeleteResult(false, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar material: {ex.Message}");
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
