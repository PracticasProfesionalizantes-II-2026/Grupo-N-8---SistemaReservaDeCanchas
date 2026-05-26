using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IMaterial_DeportivoRepository
{
    Task<IEnumerable<Material_Deportivo>> ObtenerTodos();
    Task<Material_Deportivo> ObtenerPorId(int id);
    Task Agregar(Material_Deportivo materialDeportivo);
    Task Actualizar(Material_Deportivo materialDeportivo);
    Task Eliminar(int id);
}

public class Material_DeportivoRepository : IMaterial_DeportivoRepository
{
    private readonly AppDbContext _db;

    public Material_DeportivoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Material_Deportivo>> ObtenerTodos()
    {
        return await _db.MaterialesDeportivos.ToListAsync();
    }
    public async Task<Material_Deportivo> ObtenerPorId(int id)
    {
        return await _db.MaterialesDeportivos.FindAsync(id);
    }
    public async Task Agregar(Material_Deportivo materialDeportivo)
    {
        _db.MaterialesDeportivos.Add(materialDeportivo);
        await _db.SaveChangesAsync();
    }
    public async Task Actualizar(Material_Deportivo materialDeportivo)
    {
        _db.MaterialesDeportivos.Update(materialDeportivo);
        await _db.SaveChangesAsync();
    }
    public async Task Eliminar(int id)
    {
        var materialDeportivo = await _db.MaterialesDeportivos.FindAsync(id);
        if (materialDeportivo != null)
        {
            _db.MaterialesDeportivos.Remove(materialDeportivo);
            await _db.SaveChangesAsync();
        }
    }
}