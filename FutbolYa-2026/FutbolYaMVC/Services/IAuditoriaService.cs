using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IAuditoriaService
{
    Task<List<AuditoriaResponse>> GetAllAsync();
}
