using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IProductoService
{
    Task<List<ProductoResponse>> GetAllAsync();
    Task<(ProductoResponse? producto, string? error)> CreateAsync(ProductoCreateRequest request);
    Task<(ProductoResponse? producto, string? error)> UpdateAsync(int id, ProductoUpdateRequest request);
    Task<(ProductoResponse? producto, string? error)> AjustarStockAsync(int id, int ajuste);
    Task<(ProductoResponse? producto, string? error)> ReactivarAsync(int id);
    Task<DeleteResult> DeleteAsync(int id);
}
