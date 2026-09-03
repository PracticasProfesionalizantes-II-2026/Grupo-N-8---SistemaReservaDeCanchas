using FutbolyaMVC.DTOs;
using System.Text.Json;

namespace FutbolyaMVC.Services;

public class ProductoService : IProductoService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5007";

    public ProductoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductoResponse>> GetAllAsync()
    {
        try
        {
            var productos = await _httpClient.GetFromJsonAsync<List<ProductoResponse>>(
                $"{_apiBaseUrl}/api/productos");

            return productos ?? new List<ProductoResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener productos: {ex.Message}");
            return new List<ProductoResponse>();
        }
    }

    public async Task<(ProductoResponse? producto, string? error)> CreateAsync(ProductoCreateRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/productos", request);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ProductoResponse>(), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear producto: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<(ProductoResponse? producto, string? error)> UpdateAsync(int id, ProductoCreateRequest request)
    {
        try
        {
            // La API expone PUT (reemplazo completo) para productos, a diferencia de Canchas que usa PATCH.
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/productos/{id}", request);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ProductoResponse>(), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al editar producto: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/productos/{id}");

            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

            // 404: no existe / 409: tiene ventas asociadas. La API devuelve { "mensaje": "..." }.
            return new DeleteResult(false, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar producto: {ex.Message}");
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
