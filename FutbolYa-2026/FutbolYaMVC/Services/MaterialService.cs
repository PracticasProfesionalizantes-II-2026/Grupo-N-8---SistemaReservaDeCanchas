using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;
using System.Text.Json;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/materiales; cada método parsea el error de la API con LeerMensajeError.
public class MaterialService : IMaterialService
{
    private readonly HttpClient _httpClient;

    public MaterialService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MaterialResponse>> GetAllAsync()
    {
        try
        {
            var materiales = await _httpClient.GetFromJsonAsync<List<MaterialResponse>>("/api/materiales?incluirBajas=true", JsonDefaults.Options);
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
            var response = await _httpClient.PostAsJsonAsync("/api/materiales", request, JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<MaterialResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear material: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<(MaterialResponse? material, string? error)> UpdateAsync(int id, MaterialUpdateRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/materiales/{id}", request, JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<MaterialResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al editar material: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    // Ajuste relativo de stock (+n ingreso / -n egreso).
    public async Task<(MaterialResponse? material, string? error)> AjustarStockAsync(int id, int ajuste)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/materiales/{id}/stock", new AjustarStockRequest(ajuste), JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<MaterialResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al ajustar stock de material: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<(MaterialResponse? material, string? error)> ReactivarAsync(int id)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"/api/materiales/{id}/reactivar", null);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<MaterialResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al reactivar material: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/materiales/{id}");

            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

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
