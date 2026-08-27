using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface ICanchaRepository
{
    Task<IEnumerable<Cancha>> ObtenerTodos();
    Task<Cancha?> ObtenerPorId(int id);
    Task Agregar(Cancha cancha);
    Task Actualizar(Cancha cancha);
    Task Eliminar(Cancha cancha);
}

public class CanchaRepository : ICanchaRepository
{
    private readonly AppDbContext _db;

    public CanchaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Cancha>> ObtenerTodos()
    {
        return await _db.Canchas.ToListAsync();
    }

    public async Task<Cancha?> ObtenerPorId(int id)
    {
        return await _db.Canchas.FindAsync(id);
    }

    public async Task Agregar(Cancha cancha)
    {
        _db.Canchas.Add(cancha);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Cancha cancha)
    {
        _db.Canchas.Update(cancha);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Cancha cancha)
    {
        _db.Canchas.Remove(cancha);
        await _db.SaveChangesAsync();
    }
}