using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IReservaMaterialLogica
{
    Task<IEnumerable<ReservaMaterialDto>> ObtenerTodos();
    Task<ReservaMaterialDto?> ObtenerPorId(int id);
    Task<int> Crear(ReservaMaterialCreateDto dto);
    Task<bool> Actualizar(int id, ReservaMaterialCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class ReservaMaterialLogica : IReservaMaterialLogica
{
    private readonly IReservaMaterialRepository _repo;

    public ReservaMaterialLogica(IReservaMaterialRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<ReservaMaterialDto>> ObtenerTodos()
    {
        var items = await _repo.ObtenerTodos();
        return items.Select(rm => new ReservaMaterialDto(
            rm.Cod_Reserva_Mat,
            rm.Cod_Reserva,
            rm.Cod_Material,
            rm.Cantidad
        ));
    }

    public async Task<ReservaMaterialDto?> ObtenerPorId(int id)
    {
        var rm = await _repo.ObtenerPorId(id);
        if (rm == null) return null;

        return new ReservaMaterialDto(
            rm.Cod_Reserva_Mat,
            rm.Cod_Reserva,
            rm.Cod_Material,
            rm.Cantidad
        );
    }

    public async Task<int> Crear(ReservaMaterialCreateDto dto)
    {
        var item = new Reserva_Material
        {
            Cod_Reserva = dto.Cod_Reserva,
            Cod_Material = dto.Cod_Material,
            Cantidad = dto.Cantidad
        };
        await _repo.Agregar(item);
        return item.Cod_Reserva_Mat;
    }

    public async Task<bool> Actualizar(int id, ReservaMaterialCreateDto dto)
    {
        var item = await _repo.ObtenerPorId(id);
        if (item == null) return false;

        item.Cod_Reserva = dto.Cod_Reserva;
        item.Cod_Material = dto.Cod_Material;
        item.Cantidad = dto.Cantidad;

        await _repo.Actualizar(item);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var item = await _repo.ObtenerPorId(id);
        if (item == null) return false;

        await _repo.Eliminar(item);
        return true;
    }
}