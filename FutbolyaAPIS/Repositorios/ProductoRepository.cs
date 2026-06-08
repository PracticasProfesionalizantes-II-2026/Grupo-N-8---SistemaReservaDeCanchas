using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IProductoRepository
{
    Task<IEnumerable<Producto>> ObtenerTodos();
    Task<Producto?> ObtenerPorId(int id);
    Task Agregar(Producto producto);
    Task Actualizar(Producto producto);
    Task Eliminar(Producto producto);
}

public class ProductoRepository : IProductoRepository
{
    private readonly AppDbContext _db;

    public ProductoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Producto>> ObtenerTodos()
    {
        return await _db.Productos.ToListAsync();
    }

    public async Task<Producto?> ObtenerPorId(int id)
    {
        return await _db.Productos.FindAsync(id);
    }

    public async Task Agregar(Producto producto)
    {
        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Producto producto)
    {
        _db.Productos.Update(producto);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Producto producto)
    {
        _db.Productos.Remove(producto);
        await _db.SaveChangesAsync();
    }
}