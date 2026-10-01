using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Repositorios;

public interface IMaterialDeportivoRepository
{
    Task<IEnumerable<Material_Deportivo>> ObtenerTodos();
    Task<Material_Deportivo?> ObtenerPorId(int id);
    Task Agregar(Material_Deportivo material);
    Task Actualizar(Material_Deportivo material);

    // Mismo patrón atómico que ProductoRepository.DescontarStock/RestaurarStock: ExecuteUpdateAsync
    // con WHERE condicional evita que el stock quede negativo bajo carga concurrente;
    // false indica que no había stock suficiente y no se actualizó ninguna fila.
    Task<bool> DescontarStock(int id, int cantidad);
    Task RestaurarStock(int id, int cantidad);
}

public class MaterialDeportivoRepository : IMaterialDeportivoRepository
{
    private readonly AppDbContext _db;

    public MaterialDeportivoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Material_Deportivo>> ObtenerTodos()
    {
        return await _db.MaterialesDeportivos.ToListAsync();
    }

    public async Task<Material_Deportivo?> ObtenerPorId(int id)
    {
        return await _db.MaterialesDeportivos.FindAsync(id);
    }

    public async Task Agregar(Material_Deportivo material)
    {
        _db.MaterialesDeportivos.Add(material);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Material_Deportivo material)
    {
        _db.MaterialesDeportivos.Update(material);
        await _db.SaveChangesAsync();
    }

    // Ver el comentario en ProductoRepository.DescontarStock: no volver a llamar Actualizar(...)
    // sobre un Material_Deportivo ya cargado en este contexto después de usar estos métodos.
    public async Task<bool> DescontarStock(int id, int cantidad)
    {
        var filasAfectadas = await _db.MaterialesDeportivos
            .Where(m => m.Cod_Material == id && m.Cant_Material >= cantidad)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Cant_Material, m => m.Cant_Material - cantidad));

        await SincronizarInstanciaCargada(id);
        return filasAfectadas > 0;
    }

    public async Task RestaurarStock(int id, int cantidad)
    {
        await _db.MaterialesDeportivos
            .Where(m => m.Cod_Material == id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Cant_Material, m => m.Cant_Material + cantidad));

        await SincronizarInstanciaCargada(id);
    }

    // Ver ProductoRepository.SincronizarInstanciaCargada: evita devolver Cant_Material vieja
    // desde el change tracker después de un update atómico.
    private async Task SincronizarInstanciaCargada(int id)
    {
        var cargado = _db.MaterialesDeportivos.Local.FirstOrDefault(m => m.Cod_Material == id);
        if (cargado != null)
            await _db.Entry(cargado).ReloadAsync();
    }
}