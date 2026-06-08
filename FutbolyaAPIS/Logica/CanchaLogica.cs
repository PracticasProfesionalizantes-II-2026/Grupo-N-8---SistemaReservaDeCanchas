using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface ICanchaLogica
{
    Task<IEnumerable<CanchaDto>> ObtenerTodos();
    Task<CanchaDto?> ObtenerPorId(int id);
    Task<int> Crear(CanchaCreateDto dto);
    Task<bool> ActualizarDescripcion(int id, string descripcion);
    Task<bool> ActualizarEstado(int id, bool estado);
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
            Descripcion = dto.Descripcion,
            Nombre      = "Cancha N° 0",  // temporal hasta obtener el Id
            Estado      = true            // true = Disponible por defecto
        };

        await _repo.Agregar(cancha);

        cancha.Nombre = $"Cancha N° {cancha.Cod_Cancha}";
        await _repo.Actualizar(cancha);

        return cancha.Cod_Cancha;
    }

    public async Task<bool> ActualizarDescripcion(int id, string descripcion)
    {
        var cancha = await _repo.ObtenerPorId(id);
        if (cancha == null) return false;

        cancha.Descripcion = descripcion;

        await _repo.Actualizar(cancha);
        return true;
    }

    public async Task<bool> ActualizarEstado(int id, bool estado)
    {
        var cancha = await _repo.ObtenerPorId(id);
        if (cancha == null) return false;

        cancha.Estado = estado;

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