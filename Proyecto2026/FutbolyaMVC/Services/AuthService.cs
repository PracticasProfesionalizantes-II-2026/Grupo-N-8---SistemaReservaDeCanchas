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
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/usuarios/api/auth/login",
                request
            );

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                using var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                var codUsuario = root.GetProperty("cod_Usuario").GetInt32();
                var nombre = root.GetProperty("nombre").GetString() ?? "Usuario";
                var rol = root.GetProperty("rol").GetBoolean();
                var cambiarContraseña = root.GetProperty("cambiar_Contraseña").GetBoolean();

                return new LoginResponse(codUsuario, nombre, rol, cambiarContraseña);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en login: {ex.Message}");
            return null;
        }
    }
}