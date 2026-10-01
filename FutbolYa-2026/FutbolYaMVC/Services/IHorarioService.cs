using FutbolYaMVC.DTOs;

namespace FutbolYaMVC.Services;

public interface IHorarioService
{
    Task<List<HorarioResponse>> GetAllAsync();
}
