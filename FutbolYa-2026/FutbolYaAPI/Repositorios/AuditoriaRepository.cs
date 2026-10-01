using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Repositorios;

public interface IAuditoriaRepository
{
    Task<IEnumerable<Auditoria>> ObtenerTodos(string? entidad, int? codUsuario);
    Task Agregar(Auditoria auditoria);
}

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly AppDbContext _db;

    public AuditoriaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Auditoria>> ObtenerTodos(string? entidad, int? codUsuario)
    {
        var query = _db.Auditorias.Include(a => a.Usuario).AsQueryable();

        if (!string.IsNullOrWhiteSpace(entidad))
            query = query.Where(a => a.Entidad_Afectada == entidad);

        if (codUsuario.HasValue)
            query = query.Where(a => a.Cod_Usuario == codUsuario.Value);

        return await query.OrderByDescending(a => a.Fecha_Hora).ToListAsync();
    }

    public async Task Agregar(Auditoria auditoria)
    {
        _db.Auditorias.Add(auditoria);
        await _db.SaveChangesAsync();
    }
}
