using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IReservaMaterialRepository
{
    Task<IEnumerable<Reserva_Material>> ObtenerTodos();
    Task<Reserva_Material?> ObtenerPorId(int id);
    Task Agregar(Reserva_Material reservaMaterial);
    Task Actualizar(Reserva_Material reservaMaterial);
    Task Eliminar(Reserva_Material reservaMaterial);
}

public class ReservaMaterialRepository : IReservaMaterialRepository
{
    private readonly AppDbContext _db;

    public ReservaMaterialRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Reserva_Material>> ObtenerTodos()
    {
        return await _db.ReservasMateriales
                        .Include(rm => rm.Reserva)
                        .Include(rm => rm.Material_Deportivo)
                        .ToListAsync();
    }

    public async Task<Reserva_Material?> ObtenerPorId(int id)
    {
        return await _db.ReservasMateriales
                        .Include(rm => rm.Reserva)
                        .Include(rm => rm.Material_Deportivo)
                        .FirstOrDefaultAsync(rm => rm.Cod_Reserva_Mat == id);
    }

    public async Task Agregar(Reserva_Material reservaMaterial)
    {
        _db.ReservasMateriales.Add(reservaMaterial);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Reserva_Material reservaMaterial)
    {
        _db.ReservasMateriales.Update(reservaMaterial);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Reserva_Material reservaMaterial)
    {
        _db.ReservasMateriales.Remove(reservaMaterial);
        await _db.SaveChangesAsync();
    }
}