using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IVentaRepository
{
    Task<IEnumerable<Venta>> ObtenerTodos();
    Task<Venta>ObtenerPorID(int id);
    Task Agregar(Venta venta);
    Task Actualizar(Venta venta);
    Task Eliminar(int id);
}

public class VentaRepository : IVentaRepository
{
    private readonly AppDbContext _db;
    public VentaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Venta>> ObtenerTodos()
    {
        return await _db.Ventas.ToListAsync();
    }

    public async Task<Venta>ObtenerPorID(int id)
    {
        return await _db.Ventas.FindAsync(id);
    }

    public async Task Agregar(Venta venta)
    {
        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Venta venta)
    {
        _db.Ventas.Update(venta);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(int id)
    {
        var venta = await _db.Ventas.FindAsync(id);

        if(venta != null)
        {
            _db.Ventas.Remove(venta);
            await _db.SaveChangesAsync();
        }
    }

}