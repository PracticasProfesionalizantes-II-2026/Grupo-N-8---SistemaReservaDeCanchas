using FutbolyaMVC.DTOs;
using System.Text.Json;

namespace FutbolyaMVC.Services;

public class VentaService : IVentaService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5007";

    public VentaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<VentaResponse>> GetAllAsync()
    {
        try
        {
            var ventas = await _httpClient.GetFromJsonAsync<List<VentaResponse>>(
                $"{_apiBaseUrl}/api/ventas");

            // Orden ascendente por código de venta, tal como se ve en el mockup.
            return (ventas ?? new List<VentaResponse>())
                .OrderBy(v => v.Cod_Venta)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener ventas: {ex.Message}");
            return new List<VentaResponse>();
        }
    }

    public async Task<VentaResponse?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/ventas/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<VentaResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la venta {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<(VentaResponse? venta, string? error)> CreateAsync(int codUsuario, VentaCreateRequest request)
    {
        try
        {
            var apiRequest = new VentaCreateApiRequest(codUsuario, request.Detalle);
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/ventas", apiRequest);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<VentaResponse>(), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al registrar la venta: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/ventas/{id}");

            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

            return new DeleteResult(false, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar la venta: {ex.Message}");
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
