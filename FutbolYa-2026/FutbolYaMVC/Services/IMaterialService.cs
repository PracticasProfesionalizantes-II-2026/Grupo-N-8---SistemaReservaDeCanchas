using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IMaterialService
{
    Task<List<MaterialResponse>> GetAllAsync();
    Task<(MaterialResponse? material, string? error)> CreateAsync(MaterialCreateRequest request);
    Task<(MaterialResponse? material, string? error)> UpdateAsync(int id, MaterialUpdateRequest request);
    Task<(MaterialResponse? material, string? error)> AjustarStockAsync(int id, int ajuste);
    Task<(MaterialResponse? material, string? error)> ReactivarAsync(int id);
    Task<DeleteResult> DeleteAsync(int id);
}
