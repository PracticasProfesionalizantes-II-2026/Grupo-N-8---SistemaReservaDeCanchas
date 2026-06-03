using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IReservaRepository
{
    Task<IEnumerable<Reserva>> ObtenerTodos();
    Task<Reserva?> ObtenerPorId(int id);
    Task Agregar(Reserva reserva);
    Task Actualizar(Reserva reserva);
    Task Eliminar(Reserva reserva);
}

public class ReservaRepository : IReservaRepository
{
    private readonly AppDbContext _db;

    public ReservaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Reserva>> ObtenerTodos()
    {
        return await _db.Reservas
                        .Include(r => r.Usuario)
                        .Include(r => r.Cancha)
                        .Include(r => r.HorarioDisponible)
                        .Include(r => r.ReservaMateriales)
                            .ThenInclude(rm => rm.Material_Deportivo)
                        .ToListAsync();
    }

    public async Task<Reserva?> ObtenerPorId(int id)
    {
        return await _db.Reservas
                        .Include(r => r.Usuario)
                        .Include(r => r.Cancha)
                        .Include(r => r.HorarioDisponible)
                        .Include(r => r.ReservaMateriales)
                            .ThenInclude(rm => rm.Material_Deportivo)
                        .FirstOrDefaultAsync(r => r.Cod_Reserva == id);
    }

    public async Task Agregar(Reserva reserva)
    {
        _db.Reservas.Add(reserva);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Reserva reserva)
    {
        _db.Reservas.Update(reserva);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Reserva reserva)
    {
        _db.Reservas.Remove(reserva);
        await _db.SaveChangesAsync();
    }
}