using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;


namespace FutbolyaAPIS.Repositorios;
public interface IHorarioDisponibleRepository
{
    Task<IEnumerable<HorarioDisponible>> ObtenerTodos();
    Task<HorarioDisponible> ObtenerPorId(int id);
    Task Agregar(HorarioDisponible horarioDisponible);
    Task Actualizar(HorarioDisponible horarioDisponible);
    Task Eliminar(int id);
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
    public async Task<HorarioDisponible> ObtenerPorId(int id)
    {
        return await _db.HorariosDisponibles.FindAsync(id);
    }
    public async Task Agregar(HorarioDisponible horarioDisponible)
    {
        _db.HorariosDisponibles.Add(horarioDisponible);
        await _db.SaveChangesAsync();
    }
    public async Task Actualizar(HorarioDisponible horarioDisponible)
    {
        _db.HorariosDisponibles.Update(horarioDisponible);
        await _db.SaveChangesAsync();
    }
    public async Task Eliminar(int id)
    {
        var horarioDisponible = await _db.HorariosDisponibles.FindAsync(id);
        if (horarioDisponible != null)
        {
            _db.HorariosDisponibles.Remove(horarioDisponible);
            await _db.SaveChangesAsync();
        }
    }
}