using FutbolyaMVC.DTOs;
using System.Text.Json;

namespace FutbolyaMVC.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5007";

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            // Enviamos la petición POST a la API
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/usuarios/api/auth/login", 
                request
            );

            // Si la petición fue exitosa, deserializamos la respuesta
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LoginResponse>(content);
            }

            // Si falla, devolvemos null
            return null;
        }
        catch (Exception ex)
        {
            // Si hay error de conexión, lo registramos y devolvemos null
            Console.WriteLine($"Error en login: {ex.Message}");
            return null;
        }
    }
}