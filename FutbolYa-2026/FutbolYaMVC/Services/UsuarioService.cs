using System.Net.Http.Json;
using System.Text.Json;
using FutbolYaMVC.DTOs;
using FutbolYaMVC.Infraestructura;

namespace FutbolYaMVC.Services;

// HttpClient tipado hacia /api/usuarios; mismo patrón que el resto de los Services.
public class UsuarioService : IUsuarioService
{
    private readonly HttpClient _httpClient;

    public UsuarioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UsuarioResponse>> GetAllAsync()
    {
        try
        {
            var usuarios = await _httpClient.GetFromJsonAsync<List<UsuarioResponse>>("/api/usuarios?incluirBajas=true", JsonDefaults.Options);
            return usuarios ?? new List<UsuarioResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener usuarios: {ex.Message}");
            return new List<UsuarioResponse>();
        }
    }

    public async Task<(UsuarioResponse? usuario, string? error, int? codUsuarioInactivo)> CreateAsync(UsuarioCreateRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/usuarios", request, JsonDefaults.Options);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<UsuarioResponse>(JsonDefaults.Options), null, null);

            var (mensaje, codInactivo) = await LeerMensajeConflicto(response);
            return (null, mensaje, codInactivo);
        }
        catch (Exception ex)
        {
            return (null, $"Error de conexión con el servidor: {ex.Message}", null);
        }
    }

    // Reactivar un usuario dado de baja en vez de crear un duplicado con el mismo DNI/correo.
    public async Task<(UsuarioResponse? usuario, string? error)> ReactivarAsync(int id, UsuarioCreateRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/usuarios/{id}/reactivar", request, JsonDefaults.Options);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<UsuarioResponse>(JsonDefaults.Options), null);

            return (null, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            return (null, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    public async Task<(UsuarioResponse? usuario, string? error)> UpdateAsync(int id, UsuarioUpdateRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/usuarios/{id}", request, JsonDefaults.Options);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<UsuarioResponse>(JsonDefaults.Options), null);

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
            var response = await _httpClient.DeleteAsync($"/api/usuarios/{id}");
            if (response.IsSuccessStatusCode)
                return new DeleteResult(true, null);

            return new DeleteResult(false, await LeerMensajeError(response));
        }
        catch (Exception ex)
        {
            return new DeleteResult(false, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    public async Task<(bool exitoso, string? mensaje)> ResetearContrasenaAsync(int id)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"/api/usuarios/{id}/resetear-contrasena", null);
            var mensaje = response.IsSuccessStatusCode
                ? (await response.Content.ReadFromJsonAsync<MensajeResponse>(JsonDefaults.Options))?.Mensaje
                : await LeerMensajeError(response);

            return (response.IsSuccessStatusCode, mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión con el servidor: {ex.Message}");
        }
    }

    public async Task<(bool exitoso, string? mensaje)> CambiarContrasenaAsync(int id, CambiarContrasenaRequest request)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"/api/usuarios/{id}/contrasena", request, JsonDefaults.Options);
            var mensaje = response.IsSuccessStatusCode
                ? (await response.Content.ReadFromJsonAsync<MensajeResponse>(JsonDefaults.Options))?.Mensaje
                : await LeerMensajeError(response);

            return (response.IsSuccessStatusCode, mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión con el servidor: {ex.Message}");
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

    private static async Task<(string mensaje, int? codUsuarioInactivo)> LeerMensajeConflicto(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var mensaje = jsonDoc.RootElement.TryGetProperty("mensaje", out var m)
                ? (m.GetString() ?? "Ocurrió un error")
                : "Ocurrió un error";
            var codInactivo = jsonDoc.RootElement.TryGetProperty("cod_usuario_inactivo", out var c) && c.ValueKind == JsonValueKind.Number
                ? c.GetInt32()
                : (int?)null;
            return (mensaje, codInactivo);
        }
        catch
        {
            return ("Ocurrió un error", null);
        }
    }
}
