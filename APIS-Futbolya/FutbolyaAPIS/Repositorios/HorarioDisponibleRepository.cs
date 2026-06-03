using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IHorarioDisponibleRepository
{
    Task<IEnumerable<HorarioDisponible>> ObtenerTodos();
    Task<HorarioDisponible?> ObtenerPorId(int id);
    Task Agregar(HorarioDisponible horario);
    Task Actualizar(HorarioDisponible horario);
    Task Eliminar(HorarioDisponible horario);
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
        return await _db.HorariosDisponibles.ToListAsync();
    }

    public async Task<HorarioDisponible?> ObtenerPorId(int id)
    {
        return await _db.HorariosDisponibles.FindAsync(id);
    }

    public async Task Agregar(HorarioDisponible horario)
    {
        _db.HorariosDisponibles.Add(horario);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(HorarioDisponible horario)
    {
        _db.HorariosDisponibles.Update(horario);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(HorarioDisponible horario)
    {
        _db.HorariosDisponibles.Remove(horario);
        await _db.SaveChangesAsync();
    }
}