using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IVentaDetalladaLogica
{
    Task<IEnumerable<VentaDetalladaDto>> ObtenerTodos();
    Task<VentaDetalladaDto?> ObtenerPorId(int id);
    Task<int> Crear(VentaDetalladaCreateDto dto);
    Task<bool> Actualizar(int id, VentaDetalladaCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class VentaDetalladaLogica : IVentaDetalladaLogica
{
    private readonly IVentaDetalladaRepository _repo;

    public VentaDetalladaLogica(IVentaDetalladaRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<VentaDetalladaDto>> ObtenerTodos()
    {
        var items = await _repo.ObtenerTodos();
        return items.Select(vd => new VentaDetalladaDto(
            vd.Cod_Venta_Detallada,
            vd.Cod_Venta,
            vd.Cod_Producto,
            vd.Cantidad,
            vd.Precio,
            vd.SubTotal
        ));
    }

    public async Task<VentaDetalladaDto?> ObtenerPorId(int id)
    {
        var vd = await _repo.ObtenerPorId(id);
        if (vd == null) return null;

        return new VentaDetalladaDto(
            vd.Cod_Venta_Detallada,
            vd.Cod_Venta,
            vd.Cod_Producto,
            vd.Cantidad,
            vd.Precio,
            vd.SubTotal
        );
    }

    public async Task<int> Crear(VentaDetalladaCreateDto dto)
    {
        var item = new VentaDetallada
        {
            Cod_Venta = dto.Cod_Venta,
            Cod_Producto = dto.Cod_Producto,
            Cantidad = dto.Cantidad,
            Precio = dto.Precio,
            SubTotal = dto.SubTotal
        };
        await _repo.Agregar(item);
        return item.Cod_Venta_Detallada;
    }

    public async Task<bool> Actualizar(int id, VentaDetalladaCreateDto dto)
    {
        var item = await _repo.ObtenerPorId(id);
        if (item == null) return false;

        item.Cod_Venta = dto.Cod_Venta;
        item.Cod_Producto = dto.Cod_Producto;
        item.Cantidad = dto.Cantidad;
        item.Precio = dto.Precio;
        item.SubTotal = dto.SubTotal;

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