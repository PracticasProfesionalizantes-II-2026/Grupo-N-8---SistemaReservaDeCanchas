using System.Diagnostics.Eventing.Reader;
using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.Dtos;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;



public interface ICanchaLogica
{
    Task<IEnumerable<CanchaDto>>ObtenerTodos();
    Task<CanchaDto>ObtenerPorId(int id);
    Task<int>Crear(CachaCreateDto dto);
    Task<bool>Actualizar(int id, CachaCreateDto dto);
}


public class CanchaLogica : ICanchaLogica
{
    private readonly ICanchaRepository _repo;

    public CanchaLogica(ICanchaRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<CanchaDto>>ObtenerTodos()
    {
        var cancha = await _repo.ObtenerTodos();

        return cancha.Select(c => new CanchaDto(
            c.Cod_Cancha,
            c.Nombre,
            c.Descripcion,
            c.Estado
        ));
    }

    public async Task<CanchaDto?>ObtenerPorId(int id)
    {
        var cancha = await _repo.ObtenerPorId(id);
        if(cancha == null)
        {
            return null;
        }

        return new CanchaDto(
            cancha.Cod_Cancha,
            cancha.Nombre,
            cancha.Descripcion,
            cancha.Estado
        );

    }

    public async Task<int>Crear(CachaCreateDto dto)
    {
        var cancha = new Cancha
        {
            Descripcion = dto.Descripcion
        };
        await _repo.Agregar(cancha);
        return cancha.Cod_Cancha;
    }

    public async Task<bool>Actualizar(int id, CachaCreateDto dto)
    {
        var cancha = await _repo.ObtenerPorId(id);

        if (cancha == null)
        {
            return false;
        }

        cancha.Descripcion = dto.Descripcion;

        await _repo.Actualizar(cancha);

        return true;
    }
    public async Task<bool>Eliminar(int id, CachaCreateDto dto)
    {
        var cancha = await _repo.ObtenerPorId(id);

        if(cancha == null)
        {
            return false;
        }
        
        await _repo.Eliminar(cancha.Cod_Cancha);
        return true;
    }
}