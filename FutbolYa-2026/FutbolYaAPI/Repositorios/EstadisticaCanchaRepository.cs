using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;

namespace FutbolYaAPI.Repositorios;

// Fila "plana" de reserva, ya con el nombre de la cancha y el horario resueltos.
// Cod_Reserva se incluye porque una misma reserva puede ocupar varios bloques horarios
// (una fila por bloque): hay que contar reservas distintas, no filas, para no inflar el total.
public record ReservaFila(int Cod_Reserva, DateTime FechaReserva, int Cod_Cancha, string Cancha, TimeSpan HoraInicio);

public interface IEstadisticaCanchaRepository
{
    Task<List<ReservaFila>> ObtenerReservas(DateTime desde, DateTime hasta, List<int>? canchas);
    Task<List<(int Cod_Cancha, string Nombre)>> ObtenerCanchas(List<int>? canchas);
    Task<List<TimeSpan>> ObtenerHorariosActivos();
}

public class EstadisticaCanchaRepository : IEstadisticaCanchaRepository
{
    private readonly AppDbContext _db;

    public EstadisticaCanchaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReservaFila>> ObtenerReservas(DateTime desde, DateTime hasta, List<int>? canchas)
    {
        // Una fila por cada bloque horario reservado (una Reserva puede tener varios bloques).
        // Se excluyen las reservas Canceladas: no reflejan uso real de la cancha. Se incluyen
        // tanto Pendiente (todavía no ocurrió) como Confirmada (ya ocurrió) — las dos representan
        // uso real, la diferencia es solo si ya pasó la fecha/hora o no.
        var query = _db.ReservasHorarios
                       .Include(rh => rh.Reserva)
                           .ThenInclude(r => r.Cancha)
                       .Include(rh => rh.HorarioDisponible)
                       .Where(rh => rh.Reserva.Estado != Entidades.EstadoReserva.Cancelada
                                 && rh.Reserva.FechaReserva >= desde.Date
                                 && rh.Reserva.FechaReserva <= hasta.Date);

        if (canchas != null && canchas.Count > 0)
            query = query.Where(rh => canchas.Contains(rh.Reserva.Cod_Cancha));

        return await query
            .Select(rh => new ReservaFila(rh.Cod_Reserva, rh.Reserva.FechaReserva, rh.Reserva.Cod_Cancha, rh.Reserva.Cancha.Nombre, rh.HorarioDisponible.HoraInicio))
            .ToListAsync();
    }

    public async Task<List<(int Cod_Cancha, string Nombre)>> ObtenerCanchas(List<int>? canchas)
    {
        var query = _db.Canchas.AsQueryable();
        if (canchas != null && canchas.Count > 0)
            query = query.Where(c => canchas.Contains(c.Cod_Cancha));

        var lista = await query.OrderBy(c => c.Cod_Cancha).ToListAsync();
        return lista.Select(c => (c.Cod_Cancha, c.Nombre)).ToList();
    }

    public async Task<List<TimeSpan>> ObtenerHorariosActivos()
    {
        return await _db.HorariosDisponibles
                        .Where(h => h.Activo)
                        .OrderBy(h => h.HoraInicio)
                        .Select(h => h.HoraInicio)
                        .ToListAsync();
    }
}
