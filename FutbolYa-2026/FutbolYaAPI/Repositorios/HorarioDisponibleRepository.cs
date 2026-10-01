using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Repositorios;

public interface IHorarioDisponibleRepository
{
    Task<IEnumerable<HorarioDisponible>> ObtenerTodos();
    Task<IEnumerable<HorarioDisponible>> ObtenerPorCancha(int codCancha);
    Task<HorarioDisponible?> ObtenerPorId(int id);
    Task Actualizar(HorarioDisponible horario);
}

public class HorarioDisponibleRepository : IHorarioDisponibleRepository
{
    private readonly AppDbContext _db;

    public HorarioDisponibleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<HorarioDisponible>> ObtenerTodos()
    {
        return await _db.HorariosDisponibles.OrderBy(h => h.HoraInicio).ToListAsync();
    }

    // Los horarios son un catálogo global, no pertenecen a una cancha; esto devuelve
    // los bloques que esa cancha en particular ofrece, vía la tabla intermedia Cancha_Horario.
    public async Task<IEnumerable<HorarioDisponible>> ObtenerPorCancha(int codCancha)
    {
        return await _db.CanchaHorarios
                        .Where(ch => ch.Cod_Cancha == codCancha)
                        .Select(ch => ch.HorarioDisponible)
                        .OrderBy(h => h.HoraInicio)
                        .ToListAsync();
    }

    public async Task<HorarioDisponible?> ObtenerPorId(int id)
    {
        return await _db.HorariosDisponibles.FindAsync(id);
    }

    public async Task Actualizar(HorarioDisponible horario)
    {
        _db.HorariosDisponibles.Update(horario);
        await _db.SaveChangesAsync();
    }
}
