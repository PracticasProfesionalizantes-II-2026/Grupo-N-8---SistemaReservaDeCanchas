using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IReservaLogica
{
    Task<IEnumerable<ReservaDto>> ObtenerTodos();
    Task<ReservaDto?> ObtenerPorId(int id);
    Task<int> Crear(ReservaCreateDto dto);
    Task<bool> Actualizar(int id, ReservaCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class ReservaLogica : IReservaLogica
{
    private readonly IReservaRepository _repo;

    public ReservaLogica(IReservaRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<ReservaDto>> ObtenerTodos()
    {
        var reservas = await _repo.ObtenerTodos();
        return reservas.Select(r => new ReservaDto(
            r.Cod_Reserva,
            r.Fecha,
            r.Dni_Cliente,
            r.Telefono_Cliente,
            r.Cod_Cancha,
            r.Cod_Usuario,
            r.Duracion,
            r.Cod_Horario
        ));
    }

    public async Task<ReservaDto?> ObtenerPorId(int id)
    {
        var r = await _repo.ObtenerPorId(id);
        if (r == null) return null;

        return new ReservaDto(
            r.Cod_Reserva,
            r.Fecha,
            r.Dni_Cliente,
            r.Telefono_Cliente,
            r.Cod_Cancha,
            r.Cod_Usuario,
            r.Duracion,
            r.Cod_Horario
        );
    }

    public async Task<int> Crear(ReservaCreateDto dto)
    {
        var reserva = new Reserva
        {
            Fecha = dto.Fecha,
            Dni_Cliente = dto.Dni_Cliente,
            Telefono_Cliente = dto.Telefono_Cliente,
            Cod_Cancha = dto.Cod_Cancha,
            Cod_Usuario = dto.Cod_Usuario,
            Duracion = dto.Duracion,
            Cod_Horario = dto.Cod_Horario
        };
        await _repo.Agregar(reserva);
        return reserva.Cod_Reserva;
    }

    public async Task<bool> Actualizar(int id, ReservaCreateDto dto)
    {
        var reserva = await _repo.ObtenerPorId(id);
        if (reserva == null) return false;

        reserva.Fecha = dto.Fecha;
        reserva.Dni_Cliente = dto.Dni_Cliente;
        reserva.Telefono_Cliente = dto.Telefono_Cliente;
        reserva.Cod_Cancha = dto.Cod_Cancha;
        reserva.Cod_Usuario = dto.Cod_Usuario;
        reserva.Duracion = dto.Duracion;
        reserva.Cod_Horario = dto.Cod_Horario;

        await _repo.Actualizar(reserva);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var reserva = await _repo.ObtenerPorId(id);
        if (reserva == null) return false;

        await _repo.Eliminar(reserva);
        return true;
    }
}