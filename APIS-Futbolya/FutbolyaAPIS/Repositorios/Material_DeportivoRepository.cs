using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IMaterialDeportivoRepository
{
    Task<IEnumerable<Material_Deportivo>> ObtenerTodos();
    Task<Material_Deportivo?> ObtenerPorId(int id);
    Task Agregar(Material_Deportivo material);
    Task Actualizar(Material_Deportivo material);
    Task Eliminar(Material_Deportivo material);
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

    public async Task Eliminar(Material_Deportivo material)
    {
        _db.MaterialesDeportivos.Remove(material);
        await _db.SaveChangesAsync();
    }
}