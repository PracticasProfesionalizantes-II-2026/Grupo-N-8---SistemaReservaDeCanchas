using FutbolyaMVC.DTOs;

namespace FutbolyaMVC.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}