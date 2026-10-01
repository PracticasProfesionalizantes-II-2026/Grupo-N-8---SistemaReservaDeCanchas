using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;

namespace FutbolYaAPI.Repositorios;

// Fila "plana" de detalle de venta, ya con el nombre del producto resuelto.
public record DetalleVentaFila(DateTime Fecha, int Cod_Producto, string Nombre_Producto, int Cantidad, decimal SubTotal);

public interface IEstadisticaRepository
{
    Task<List<DetalleVentaFila>> ObtenerDetalles(DateTime desde, DateTime hasta, List<int>? productos);
}

public class EstadisticaRepository : IEstadisticaRepository
{
    private readonly AppDbContext _db;

    public EstadisticaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DetalleVentaFila>> ObtenerDetalles(DateTime desde, DateTime hasta, List<int>? productos)
    {
        // Se excluyen las ventas anuladas: no representan ingresos reales (su stock ya fue
        // restituido). Si una venta se reactiva, vuelve a contarse.
        var query = _db.VentasDetalladas
                       .Include(vd => vd.Venta)
                       .Include(vd => vd.Producto)
                       .Where(vd => vd.Venta.Activo
                                 && vd.Venta.Fecha >= desde.Date
                                 && vd.Venta.Fecha <= hasta.Date);

        if (productos != null && productos.Count > 0)
            query = query.Where(vd => productos.Contains(vd.Cod_Producto));

        return await query
            .Select(vd => new DetalleVentaFila(
                vd.Venta.Fecha,
                vd.Cod_Producto,
                vd.Producto.Nombre,
                vd.Cantidad,
                vd.SubTotal))
            .ToListAsync();
    }
}
