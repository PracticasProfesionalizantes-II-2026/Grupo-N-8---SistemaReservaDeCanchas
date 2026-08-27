using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Datos;
using FutbolyaAPIS.Entidades;

namespace FutbolyaAPIS.Repositorios;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ObtenerTodos();
    Task<Usuario?> ObtenerPorId(int id);
    Task Agregar(Usuario usuario);
    Task Actualizar(Usuario usuario);
    Task Eliminar(Usuario usuario);
}

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _db;

    public UsuarioRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Usuario>> ObtenerTodos()
    {
        return await _db.Usuarios.ToListAsync();
    }

    public async Task<Usuario?> ObtenerPorId(int id)
    {
        return await _db.Usuarios.FindAsync(id);
    }

    public async Task Agregar(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Usuario usuario)
    {
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Usuario usuario)
    {
        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
    }
}