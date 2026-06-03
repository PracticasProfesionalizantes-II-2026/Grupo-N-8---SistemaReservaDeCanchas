using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IVentaDetalladaRepository
{
    Task<IEnumerable<VentaDetallada>> ObtenerTodos();
    Task<VentaDetallada?> ObtenerPorId(int id);
    Task Agregar(VentaDetallada ventaDetallada);
    Task Actualizar(VentaDetallada ventaDetallada);
    Task Eliminar(VentaDetallada ventaDetallada);
}

public class VentaDetalladaRepository : IVentaDetalladaRepository
{
    private readonly AppDbContext _db;

    public VentaDetalladaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<VentaDetallada>> ObtenerTodos()
    {
        return await _db.VentasDetalladas
                        .Include(vd => vd.Venta)
                        .Include(vd => vd.Producto)
                        .ToListAsync();
    }

    public async Task<VentaDetallada?> ObtenerPorId(int id)
    {
        return await _db.VentasDetalladas
                        .Include(vd => vd.Venta)
                        .Include(vd => vd.Producto)
                        .FirstOrDefaultAsync(vd => vd.Cod_Venta_Detallada == id);
    }

    public async Task Agregar(VentaDetallada ventaDetallada)
    {
        _db.VentasDetalladas.Add(ventaDetallada);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(VentaDetallada ventaDetallada)
    {
        _db.VentasDetalladas.Update(ventaDetallada);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(VentaDetallada ventaDetallada)
    {
        _db.VentasDetalladas.Remove(ventaDetallada);
        await _db.SaveChangesAsync();
    }
}