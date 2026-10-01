using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IAuthService
{
    Task<(LoginResponse? resultado, string? mensaje)> LoginAsync(LoginRequest request);
    Task<(bool exitoso, string? mensaje)> SolicitarRecuperacionAsync(string correo);
    Task<(bool exitoso, string? mensaje)> ConfirmarRecuperacionAsync(ConfirmarRecuperacionRequest request);
}
