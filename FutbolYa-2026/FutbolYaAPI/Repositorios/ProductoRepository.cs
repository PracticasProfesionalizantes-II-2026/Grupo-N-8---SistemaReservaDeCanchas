using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Repositorios;

public interface IProductoRepository
{
    Task<IEnumerable<Producto>> ObtenerTodos();
    Task<Producto?> ObtenerPorId(int id);
    Task Agregar(Producto producto);
    Task Actualizar(Producto producto);

    // Update condicional atómico en SQL Server (WHERE Cantidad >= cantidad),
    // en vez de leer-verificar-y-grabar desde C#. Devuelve false (0 filas afectadas) si no
    // había stock suficiente en ese instante, incluso bajo carga concurrente.
    Task<bool> DescontarStock(int id, int cantidad);

    // Incremento atómico (no necesita condición: sumar nunca deja el stock en negativo).
    Task RestaurarStock(int id, int cantidad);
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

    // ExecuteUpdateAsync impacta directo en la base y no pasa por el change tracker.
    // Por eso, después de llamar a este método (o a RestaurarStock) no hay que volver a
    // llamar Actualizar(...) sobre una instancia de Producto ya cargada en este mismo
    // contexto: pisaría el update atómico con la Cantidad vieja que tenía en memoria.
    public async Task<bool> DescontarStock(int id, int cantidad)
    {
        var filasAfectadas = await _db.Productos
            .Where(p => p.Cod_Producto == id && p.Cantidad >= cantidad)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Cantidad, p => p.Cantidad - cantidad));

        await SincronizarInstanciaCargada(id);
        return filasAfectadas > 0;
    }

    public async Task RestaurarStock(int id, int cantidad)
    {
        await _db.Productos
            .Where(p => p.Cod_Producto == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Cantidad, p => p.Cantidad + cantidad));

        await SincronizarInstanciaCargada(id);
    }

    // Si el Producto ya estaba cargado en este contexto, FindAsync sigue devolviendo la
    // instancia con la Cantidad vieja después del update atómico (la respuesta y la
    // auditoría mostrarían el stock anterior). Se recarga desde la base para evitarlo.
    private async Task SincronizarInstanciaCargada(int id)
    {
        var cargado = _db.Productos.Local.FirstOrDefault(p => p.Cod_Producto == id);
        if (cargado != null)
            await _db.Entry(cargado).ReloadAsync();
    }
}