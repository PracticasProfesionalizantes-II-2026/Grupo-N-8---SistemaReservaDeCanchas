using FutbolyaMVC.DTOs;

namespace FutbolyaMVC.Services;

public record DeleteResult(bool Exitoso, string? Mensaje);

public interface ICanchaService
{
    Task<List<CanchaResponse>> GetAllAsync();
    Task<CanchaResponse?> CreateAsync(CanchaCreateRequest request);
    Task<CanchaResponse?> UpdateAsync(int id, CanchaUpdateRequest request);
    Task<DeleteResult> DeleteAsync(int id);
}