using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;
using System.Text.Json;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/productos; cada método parsea el error de la API con LeerMensajeError
// (mismo patrón que el resto de los Services — ver MaterialService para el detalle).
public class ProductoService : IProductoService
{
    private readonly HttpClient _httpClient;

    public ProductoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductoResponse>> GetAllAsync()
    {
        try
        {
            var productos = await _httpClient.GetFromJsonAsync<List<ProductoResponse>>("/api/productos?incluirBajas=true", JsonDefaults.Options);
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
            var response = await _httpClient.PostAsJsonAsync("/api/productos", request, JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ProductoResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear producto: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<(ProductoResponse? producto, string? error)> UpdateAsync(int id, ProductoUpdateRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/productos/{id}", request, JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ProductoResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al editar producto: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    // Ajuste relativo de stock (+n ingreso / -n egreso).
    public async Task<(ProductoResponse? producto, string? error)> AjustarStockAsync(int id, int ajuste)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/productos/{id}/stock", new AjustarStockRequest(ajuste), JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ProductoResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al ajustar stock de producto: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<(ProductoResponse? producto, string? error)> ReactivarAsync(int id)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"/api/productos/{id}/reactivar", null);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ProductoResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al reactivar producto: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/productos/{id}");

            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

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
