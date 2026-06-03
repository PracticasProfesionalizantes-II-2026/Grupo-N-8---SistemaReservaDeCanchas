using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IVentaLogica
{
    Task<IEnumerable<VentaDto>> ObtenerTodos();
    Task<VentaDto?> ObtenerPorId(int id);
    Task<int> Crear(VentaCreateDto dto);
    Task<bool> Actualizar(int id, VentaCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class VentaLogica : IVentaLogica
{
    private readonly IVentaRepository _repo;

    public VentaLogica(IVentaRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<VentaDto>> ObtenerTodos()
    {
        var ventas = await _repo.ObtenerTodos();
        return ventas.Select(v => new VentaDto(
            v.Cod_Venta,
            v.Fecha,
            v.Hora,
            v.MontoTotal,
            v.Cod_Usuario
        ));
    }

    public async Task<VentaDto?> ObtenerPorId(int id)
    {
        var v = await _repo.ObtenerPorId(id);
        if (v == null) return null;

        return new VentaDto(
            v.Cod_Venta,
            v.Fecha,
            v.Hora,
            v.MontoTotal,
            v.Cod_Usuario
        );
    }

    public async Task<int> Crear(VentaCreateDto dto)
    {
        var venta = new Venta
        {
            Fecha = dto.Fecha,
            Hora = dto.Hora,
            MontoTotal = dto.MontoTotal,
            Cod_Usuario = dto.Cod_Usuario
        };
        await _repo.Agregar(venta);
        return venta.Cod_Venta;
    }

    public async Task<bool> Actualizar(int id, VentaCreateDto dto)
    {
        var venta = await _repo.ObtenerPorId(id);
        if (venta == null) return false;

        venta.Fecha = dto.Fecha;
        venta.Hora = dto.Hora;
        venta.MontoTotal = dto.MontoTotal;
        venta.Cod_Usuario = dto.Cod_Usuario;

        await _repo.Actualizar(venta);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var venta = await _repo.ObtenerPorId(id);
        if (venta == null) return false;

        await _repo.Eliminar(venta);
        return true;
    }
}