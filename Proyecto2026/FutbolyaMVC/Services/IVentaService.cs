using FutbolyaMVC.DTOs;

namespace FutbolyaMVC.Services;

public interface IVentaService
{
    Task<List<VentaResponse>> GetAllAsync();
    Task<VentaResponse?> GetByIdAsync(int id);
    Task<(VentaResponse? venta, string? error)> CreateAsync(int codUsuario, VentaCreateRequest request);
    Task<DeleteResult> DeleteAsync(int id);
}
