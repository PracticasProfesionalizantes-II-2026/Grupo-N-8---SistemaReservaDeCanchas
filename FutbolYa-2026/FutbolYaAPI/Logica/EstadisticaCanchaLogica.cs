using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IEstadisticaCanchaLogica
{
    Task<EstadisticaCanchaDiaDto> ObtenerReservasDia(DateTime fecha, List<int>? canchas);
    Task<EstadisticaCanchaSemanaDto> ObtenerReservasSemana(DateTime fecha, List<int>? canchas);
    Task<EstadisticaCanchaMesDto> ObtenerReservasMes(DateTime fecha, List<int>? canchas);
}

// Agrega estadísticas de uso/reservas de canchas por día, semana y mes, con filtro opcional
// por cancha. Se ubica entre el endpoint admin-only estadisticas-cancha y EstadisticaCanchaRepository.
public class EstadisticaCanchaLogica : IEstadisticaCanchaLogica
{
    private readonly IEstadisticaCanchaRepository _repo;

    private static readonly string[] DiasSemana =
        { "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo" };

    public EstadisticaCanchaLogica(IEstadisticaCanchaRepository repo)
    {
        _repo = repo;
    }

    // ── Reporte Día: grilla Cancha x Horario ─────────────────────────────
    public async Task<EstadisticaCanchaDiaDto> ObtenerReservasDia(DateTime fecha, List<int>? canchas)
    {
        var dia = fecha.Date;
        var canchasLista = await _repo.ObtenerCanchas(canchas);
        var horarios = await _repo.ObtenerHorariosActivos();
        var reservas = await _repo.ObtenerReservas(dia, dia, canchas);

        var etiquetasHorario = horarios.Select(h => h.ToString(@"hh\:mm")).ToList();

        // El total por cancha/general cuenta reservas distintas (Cod_Reserva), no bloques
        // horarios: una reserva de 2hs no debe contar como 2 (cada celda de la grilla sí
        // representa un bloque puntual, así que ahí el conteo por bloque es correcto).
        var filas = canchasLista.Select(c =>
        {
            var reservasCancha = reservas.Where(r => r.Cod_Cancha == c.Cod_Cancha).ToList();
            var valores = horarios
                .Select(h => reservasCancha.Count(r => r.HoraInicio == h))
                .ToList();
            var totalCancha = reservasCancha.Select(r => r.Cod_Reserva).Distinct().Count();
            return new FilaCanchaDto(c.Cod_Cancha, c.Nombre, valores, totalCancha);
        }).ToList();

        var totalGeneral = reservas.Select(r => r.Cod_Reserva).Distinct().Count();
        return new EstadisticaCanchaDiaDto(dia, etiquetasHorario, filas, totalGeneral);
    }

    // ── Reporte Semana: grilla Cancha x Día (Lunes a Domingo) ─────────────
    public async Task<EstadisticaCanchaSemanaDto> ObtenerReservasSemana(DateTime fecha, List<int>? canchas)
    {
        var (desde, hasta) = RangoSemana(fecha);
        var canchasLista = await _repo.ObtenerCanchas(canchas);
        var reservas = await _repo.ObtenerReservas(desde, hasta, canchas);

        // Igual que en el reporte Día: contar reservas distintas por celda/fila/total, no bloques.
        var filas = canchasLista.Select(c =>
        {
            var reservasCancha = reservas.Where(r => r.Cod_Cancha == c.Cod_Cancha).ToList();
            var valores = Enumerable.Range(0, 7)
                .Select(i => reservasCancha
                    .Where(r => r.FechaReserva.Date == desde.AddDays(i).Date)
                    .Select(r => r.Cod_Reserva).Distinct().Count())
                .ToList();
            var totalCancha = reservasCancha.Select(r => r.Cod_Reserva).Distinct().Count();
            return new FilaCanchaDto(c.Cod_Cancha, c.Nombre, valores, totalCancha);
        }).ToList();

        var totalGeneral = reservas.Select(r => r.Cod_Reserva).Distinct().Count();
        return new EstadisticaCanchaSemanaDto(desde, hasta, DiasSemana.ToList(), filas, totalGeneral);
    }

    // ── Reporte Mes: serie diaria por cancha (día 1..N del mes) ───────────
    public async Task<EstadisticaCanchaMesDto> ObtenerReservasMes(DateTime fecha, List<int>? canchas)
    {
        var (desde, hasta) = RangoMes(fecha);
        var canchasLista = await _repo.ObtenerCanchas(canchas);
        var reservas = await _repo.ObtenerReservas(desde, hasta, canchas);

        var totalDias = (hasta - desde).Days + 1;
        var dias = Enumerable.Range(1, totalDias).ToList();

        var filas = canchasLista.Select(c =>
        {
            var reservasCancha = reservas.Where(r => r.Cod_Cancha == c.Cod_Cancha).ToList();
            var valores = dias
                .Select(d => reservasCancha
                    .Where(r => r.FechaReserva.Date == desde.AddDays(d - 1).Date)
                    .Select(r => r.Cod_Reserva).Distinct().Count())
                .ToList();
            var totalCancha = reservasCancha.Select(r => r.Cod_Reserva).Distinct().Count();
            return new FilaCanchaDto(c.Cod_Cancha, c.Nombre, valores, totalCancha);
        }).ToList();

        var totalGeneral = reservas.Select(r => r.Cod_Reserva).Distinct().Count();
        return new EstadisticaCanchaMesDto(desde, hasta, dias, filas, totalGeneral);
    }

    // ── Helpers de rango de fechas ────────────────────────────────────────
    private static (DateTime desde, DateTime hasta) RangoSemana(DateTime fecha)
    {
        var diff = (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;
        var desde = fecha.Date.AddDays(-diff);
        return (desde, desde.AddDays(6));
    }

    private static (DateTime desde, DateTime hasta) RangoMes(DateTime fecha)
    {
        var desde = new DateTime(fecha.Year, fecha.Month, 1);
        return (desde, desde.AddMonths(1).AddDays(-1));
    }
}
