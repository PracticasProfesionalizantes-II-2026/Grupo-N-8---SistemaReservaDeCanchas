using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IVentaService
{
    Task<List<VentaResponse>> GetAllAsync();
    Task<VentaResponse?> GetByIdAsync(int id);
    Task<(VentaResponse? venta, string? error)> CreateAsync(int codUsuario, VentaCreateRequest request);
    Task<(VentaResponse? venta, string? error)> ReactivarAsync(int id);
    Task<DeleteResult> DeleteAsync(int id);
}
