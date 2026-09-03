using FutbolyaMVC.DTOs;

namespace FutbolyaMVC.Services;

public interface IProductoService
{
    Task<List<ProductoResponse>> GetAllAsync();
    Task<(ProductoResponse? producto, string? error)> CreateAsync(ProductoCreateRequest request);
    Task<(ProductoResponse? producto, string? error)> UpdateAsync(int id, ProductoCreateRequest request);
    Task<DeleteResult> DeleteAsync(int id);
}
