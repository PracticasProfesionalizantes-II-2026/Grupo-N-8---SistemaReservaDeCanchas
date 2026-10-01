using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IUsuarioService
{
    Task<List<UsuarioResponse>> GetAllAsync();
    Task<(UsuarioResponse? usuario, string? error, int? codUsuarioInactivo)> CreateAsync(UsuarioCreateRequest request);
    Task<(UsuarioResponse? usuario, string? error)> UpdateAsync(int id, UsuarioUpdateRequest request);
    Task<(UsuarioResponse? usuario, string? error)> ReactivarAsync(int id, UsuarioCreateRequest request);
    Task<DeleteResult> DeleteAsync(int id);
    Task<(bool exitoso, string? mensaje)> ResetearContrasenaAsync(int id);
    Task<(bool exitoso, string? mensaje)> CambiarContrasenaAsync(int id, CambiarContrasenaRequest request);
}
