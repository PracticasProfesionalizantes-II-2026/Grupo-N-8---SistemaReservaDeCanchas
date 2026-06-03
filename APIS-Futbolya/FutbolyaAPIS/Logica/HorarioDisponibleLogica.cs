using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IHorarioDisponibleLogica
{
    Task<IEnumerable<HorarioDisponibleDto>> ObtenerTodos();
    Task<HorarioDisponibleDto?> ObtenerPorId(int id);
    Task<int> Crear(HorarioDisponibleCreateDto dto);
    Task<bool> Actualizar(int id, HorarioDisponibleCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class HorarioDisponibleLogica : IHorarioDisponibleLogica
{
    private readonly IHorarioDisponibleRepository _repo;

    public HorarioDisponibleLogica(IHorarioDisponibleRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<HorarioDisponibleDto>> ObtenerTodos()
    {
        var horarios = await _repo.ObtenerTodos();
        return horarios.Select(h => new HorarioDisponibleDto(
            h.Cod_Horario,
            h.HoraInicio,
            h.HoraFin,
            h.Activo
        ));
    }

    public async Task<HorarioDisponibleDto?> ObtenerPorId(int id)
    {
        var h = await _repo.ObtenerPorId(id);
        if (h == null) return null;

        return new HorarioDisponibleDto(
            h.Cod_Horario,
            h.HoraInicio,
            h.HoraFin,
            h.Activo
        );
    }

    public async Task<int> Crear(HorarioDisponibleCreateDto dto)
    {
        var horario = new HorarioDisponible
        {
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraFin,
            Activo = dto.Activo
        };
        await _repo.Agregar(horario);
        return horario.Cod_Horario;
    }

    public async Task<bool> Actualizar(int id, HorarioDisponibleCreateDto dto)
    {
        var horario = await _repo.ObtenerPorId(id);
        if (horario == null) return false;

        horario.HoraInicio = dto.HoraInicio;
        horario.HoraFin    = dto.HoraFin;
        horario.Activo     = dto.Activo;

        await _repo.Actualizar(horario);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var horario = await _repo.ObtenerPorId(id);
        if (horario == null) return false;

        await _repo.Eliminar(horario);
        return true;
    }
}