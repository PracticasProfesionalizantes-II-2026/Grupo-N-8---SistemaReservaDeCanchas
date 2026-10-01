using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public record DeleteResult(bool Exitoso, string? Mensaje);

public interface ICanchaService
{
    Task<List<CanchaResponse>> GetAllAsync();
    Task<(CanchaResponse? cancha, string? error)> CreateAsync(CanchaCreateRequest request);
    Task<CanchaResponse?> UpdateAsync(int id, CanchaUpdateRequest request);
    Task<(CanchaResponse? cancha, string? error)> ActualizarEstadoAsync(int id, string estado);
    Task<(CanchaResponse? cancha, string? error)> ActualizarHorariosAsync(int id, List<int> codHorarios);
    Task<DeleteResult> DeleteAsync(int id);
}
