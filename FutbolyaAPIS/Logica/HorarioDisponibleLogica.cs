using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IHorarioDisponibleLogica
{
    Task<IEnumerable<HorarioDisponibleDto>> ObtenerTodos();
    Task<HorarioDisponibleDto?> ObtenerPorId(int id);
    Task<(HorarioDisponibleDto? resultado, string? error)> Crear(HorarioDisponibleCreateDto dto);
    Task<(HorarioDisponibleDto? resultado, string? error)> Actualizar(int id, HorarioDisponibleCreateDto dto);
    Task<(HorarioDisponibleDto? resultado, string? error)> ActualizarActivo(int id, bool activo);
    Task<(bool eliminado, string? error)> Eliminar(int id);
}

public class HorarioDisponibleLogica : IHorarioDisponibleLogica
{
    private readonly IHorarioDisponibleRepository _repo;
    private readonly IReservaRepository _repoReserva;

    public HorarioDisponibleLogica(
        IHorarioDisponibleRepository repo,
        IReservaRepository repoReserva)
    {
        _repo         = repo;
        _repoReserva  = repoReserva;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static HorarioDisponibleDto MapDto(HorarioDisponible h) =>
        new HorarioDisponibleDto(
            h.Cod_Horario,
            h.HoraInicio,
            h.HoraFin,
            h.Activo
        );

    // ── Validación de solapamiento privada ─────────────────────────────
    private async Task<bool> ExisteSolapamiento(TimeSpan horaInicio, TimeSpan horaFin, int? ignorarId = null)
    {
        var horarios = await _repo.ObtenerTodos();
        return horarios.Any(h =>
            (ignorarId == null || h.Cod_Horario != ignorarId) &&
            h.HoraInicio < horaFin &&
            h.HoraFin > horaInicio
        );
    }

    public async Task<IEnumerable<HorarioDisponibleDto>> ObtenerTodos()
    {
        var horarios = await _repo.ObtenerTodos();
        return horarios.Select(MapDto);
    }

    public async Task<HorarioDisponibleDto?> ObtenerPorId(int id)
    {
        var h = await _repo.ObtenerPorId(id);
        if (h == null) return null;
        return MapDto(h);
    }

    public async Task<(HorarioDisponibleDto? resultado, string? error)> Crear(HorarioDisponibleCreateDto dto)
    {
        if (await ExisteSolapamiento(dto.HoraInicio, dto.HoraFin))
            return (null, "Ya existe un horario que se superpone con la franja horaria indicada");

        var horario = new HorarioDisponible
        {
            HoraInicio = dto.HoraInicio,
            HoraFin    = dto.HoraFin,
            Activo     = dto.Activo
        };

        await _repo.Agregar(horario);
        return (MapDto(horario), null);
    }

    public async Task<(HorarioDisponibleDto? resultado, string? error)> Actualizar(int id, HorarioDisponibleCreateDto dto)
    {
        var horario = await _repo.ObtenerPorId(id);
        if (horario == null)
            return (null, "NOT_FOUND");

        if (await ExisteSolapamiento(dto.HoraInicio, dto.HoraFin, ignorarId: id))
            return (null, "Ya existe un horario que se superpone con la franja horaria indicada");

        horario.HoraInicio = dto.HoraInicio;
        horario.HoraFin    = dto.HoraFin;
        horario.Activo     = dto.Activo;

        await _repo.Actualizar(horario);
        return (MapDto(horario), null);
    }

    public async Task<(HorarioDisponibleDto? resultado, string? error)> ActualizarActivo(int id, bool activo)
    {
        var horario = await _repo.ObtenerPorId(id);
        if (horario == null)
            return (null, "NOT_FOUND");

        // Si se intenta desactivar, verificar que no tenga reservas activas
        if (!activo)
        {
            var reservas = await _repoReserva.ObtenerTodos();
            var tieneReservas = reservas.Any(r => r.Cod_Horario == id);
            if (tieneReservas)
                return (null, "No se puede desactivar un horario con reservas activas asociadas");
        }

        horario.Activo = activo;
        await _repo.Actualizar(horario);
        return (MapDto(horario), null);
    }

    public async Task<(bool eliminado, string? error)> Eliminar(int id)
    {
        var horario = await _repo.ObtenerPorId(id);
        if (horario == null)
            return (false, "NOT_FOUND");

        var reservas = await _repoReserva.ObtenerTodos();
        if (reservas.Any(r => r.Cod_Horario == id))
            return (false, "No se puede eliminar un horario con reservas asociadas");

        await _repo.Eliminar(horario);
        return (true, null);
    }
}