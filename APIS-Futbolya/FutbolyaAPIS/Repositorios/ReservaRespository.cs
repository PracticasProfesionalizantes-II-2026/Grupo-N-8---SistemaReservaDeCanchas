using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;


namespace FutbolyaAPIS.Repositorios;


public interface IReservaRepository
{
    Task<IEnumerable<Reserva>>ObtenerTodo();
    Task<Reserva>ObtenerPorId(int id);
    Task Agregar(Reserva reserva);
    Task Actualizar(Reserva reserva);
    Task Eliminar(int id);
}


public class ReservaRespository : IReservaRepository
{
    private readonly AppDbContext _db;

    public ReservaRespository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Reserva>> ObtenerTodo()
    {
        return await _db.Reservas.ToListAsync();
    }

    public async Task<Reserva>ObtenerPorId(int id)
    {
        return await _db.Reservas.FindAsync(id);
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

    public async Task Eliminar(int id)
    {
        var reserva = await _db.Reservas.FindAsync(id);

        if(reserva != null)
        {
            _db.Reservas.Remove(reserva);
            await _db.SaveChangesAsync();
        }
    }
}