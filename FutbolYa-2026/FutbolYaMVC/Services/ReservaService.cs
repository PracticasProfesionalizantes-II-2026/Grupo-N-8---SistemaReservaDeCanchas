using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;
using System.Text.Json;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/reservas; mismo patrón que el resto de los Services.
public class ReservaService : IReservaService
{
    private readonly HttpClient _httpClient;

    public ReservaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ReservaResponse>> GetAllAsync()
    {
        try
        {
            var reservas = await _httpClient.GetFromJsonAsync<List<ReservaResponse>>("/api/reservas", JsonDefaults.Options);

            // Orden: próximas primero (por fecha de reserva y hora del primer bloque).
            return (reservas ?? new List<ReservaResponse>())
                .OrderBy(r => r.FechaReserva)
                .ThenBy(r => r.Horarios.Count > 0 ? r.Horarios[0].Hora_Inicio : TimeSpan.Zero)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener reservas: {ex.Message}");
            return new List<ReservaResponse>();
        }
    }

    public async Task<ReservaResponse?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/reservas/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ReservaResponse>(JsonDefaults.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la reserva {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<(ReservaResponse? reserva, string? error)> CreateAsync(int codUsuario, ReservaCreateRequest request)
    {
        try
        {
            var apiRequest = new ReservaCreateApiRequest(
                request.FechaReserva,
                request.Dni_Cliente,
                request.Telefono_Cliente,
                request.Cod_Cancha,
                codUsuario,
                request.Cod_Horarios,
                request.Materiales);

            var response = await _httpClient.PostAsJsonAsync("/api/reservas", apiRequest, JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ReservaResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al registrar la reserva: {ex.Message}");
            return (null, "Error de conexión con el servidor");
        }
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/reservas/{id}");

            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

            return new DeleteResult(false, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar la reserva: {ex.Message}");
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
