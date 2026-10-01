using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Repositorios;

public interface IVentaRepository
{
    Task<IEnumerable<Venta>> ObtenerTodos();
    Task<Venta?> ObtenerPorId(int id);
    Task Agregar(Venta venta);
    Task Actualizar(Venta venta);
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
        return await _db.Ventas
                        .Include(v => v.Usuario)
                        .Include(v => v.VentasDetalladas)
                        .ToListAsync();
    }

    public async Task<Venta?> ObtenerPorId(int id)
    {
        return await _db.Ventas
                        .Include(v => v.Usuario)
                        .Include(v => v.VentasDetalladas)
                            .ThenInclude(vd => vd.Producto)
                        .FirstOrDefaultAsync(v => v.Cod_Venta == id);
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
}