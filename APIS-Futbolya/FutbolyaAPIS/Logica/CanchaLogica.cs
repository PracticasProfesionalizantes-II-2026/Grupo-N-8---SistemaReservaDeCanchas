using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface ICanchaLogica
{
    Task<IEnumerable<CanchaDto>> ObtenerTodos();
    Task<CanchaDto?> ObtenerPorId(int id);
    Task<int> Crear(CanchaCreateDto dto);
    Task<bool> Actualizar(int id, CanchaCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class CanchaLogica : ICanchaLogica
{
    private readonly ICanchaRepository _repo;

    public CanchaLogica(ICanchaRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<CanchaDto>> ObtenerTodos()
    {
        var canchas = await _repo.ObtenerTodos();
        return canchas.Select(c => new CanchaDto(
            c.Cod_Cancha,
            c.Nombre,
            c.Descripcion,
            c.Estado
        ));
    }

    public async Task<CanchaDto?> ObtenerPorId(int id)
    {
        var c = await _repo.ObtenerPorId(id);
        if (c == null) return null;

        return new CanchaDto(
            c.Cod_Cancha,
            c.Nombre,
            c.Descripcion,
            c.Estado
        );
    }

    public async Task<int> Crear(CanchaCreateDto dto)
    {
        var cancha = new Cancha
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Estado = dto.Estado
        };
        await _repo.Agregar(cancha);
        return cancha.Cod_Cancha;
    }

    public async Task<bool> Actualizar(int id, CanchaCreateDto dto)
    {
        var cancha = await _repo.ObtenerPorId(id);
        if (cancha == null) return false;

        cancha.Nombre = dto.Nombre;
        cancha.Descripcion = dto.Descripcion;
        cancha.Estado = dto.Estado;

        await _repo.Actualizar(cancha);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var cancha = await _repo.ObtenerPorId(id);
        if (cancha == null) return false;

        await _repo.Eliminar(cancha);
        return true;
    }
}