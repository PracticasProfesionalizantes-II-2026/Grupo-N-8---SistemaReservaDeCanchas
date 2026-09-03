using FutbolyaMVC.DTOs;

namespace FutbolyaMVC.Services;

public interface IMaterialService
{
    Task<List<MaterialResponse>> GetAllAsync();
    Task<(MaterialResponse? material, string? error)> CreateAsync(MaterialCreateRequest request);
    Task<(MaterialResponse? material, string? error)> UpdateAsync(int id, MaterialCreateRequest request);
    Task<DeleteResult> DeleteAsync(int id);
}
