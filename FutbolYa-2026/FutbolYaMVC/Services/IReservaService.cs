using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IReservaService
{
    Task<List<ReservaResponse>> GetAllAsync();
    Task<ReservaResponse?> GetByIdAsync(int id);
    Task<(ReservaResponse? reserva, string? error)> CreateAsync(int codUsuario, ReservaCreateRequest request);
    Task<DeleteResult> DeleteAsync(int id);
}
