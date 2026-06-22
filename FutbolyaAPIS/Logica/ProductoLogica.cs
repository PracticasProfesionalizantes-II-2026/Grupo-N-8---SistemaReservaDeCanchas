using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IProductoLogica
{
    Task<IEnumerable<ProductoDto>> ObtenerTodos();
    Task<ProductoDto?> ObtenerPorId(int id);
    Task<int> Crear(ProductoCreateDto dto);
    Task<bool> Actualizar(int id, ProductoCreateDto dto);
    Task<bool> ActualizarStock(int id, int cantidad);
    Task<bool> Eliminar(int id);
}

public class ProductoLogica : IProductoLogica
{
    private readonly IProductoRepository _repo;
    private readonly IVentaDetalladaRepository _repoVentaDetallada;

    public ProductoLogica(IProductoRepository repo, IVentaDetalladaRepository repoVentaDetallada)
    {
        _repo               = repo;
        _repoVentaDetallada = repoVentaDetallada;
    }

    public async Task<IEnumerable<ProductoDto>> ObtenerTodos()
    {
        var productos = await _repo.ObtenerTodos();
        return productos.Select(p => new ProductoDto(
            p.Cod_Producto,
            p.Nombre,
            p.Cantidad,
            p.Precio,
            p.Tipo
        ));
    }

    public async Task<ProductoDto?> ObtenerPorId(int id)
    {
        var p = await _repo.ObtenerPorId(id);
        if (p == null) return null;

        return new ProductoDto(
            p.Cod_Producto,
            p.Nombre,
            p.Cantidad,
            p.Precio,
            p.Tipo
        );
    }

    public async Task<int> Crear(ProductoCreateDto dto)
    {
        var producto = new Producto
        {
            Nombre   = dto.Nombre,
            Cantidad = dto.Cantidad,
            Precio   = dto.Precio,
            Tipo     = dto.Tipo
        };
        await _repo.Agregar(producto);
        return producto.Cod_Producto;
    }

    public async Task<bool> Actualizar(int id, ProductoCreateDto dto)
    {
        var producto = await _repo.ObtenerPorId(id);
        if (producto == null) return false;

        producto.Nombre   = dto.Nombre;
        producto.Cantidad = dto.Cantidad;
        producto.Precio   = dto.Precio;
        producto.Tipo     = dto.Tipo;

        await _repo.Actualizar(producto);
        return true;
    }

    public async Task<bool> ActualizarStock(int id, int cantidad)
    {
        var producto = await _repo.ObtenerPorId(id);
        if (producto == null) return false;

        producto.Cantidad = cantidad;

        await _repo.Actualizar(producto);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var producto = await _repo.ObtenerPorId(id);
        if (producto == null) return false;

        var detalles = await _repoVentaDetallada.ObtenerTodos();
        if (detalles.Any(vd => vd.Cod_Producto == id))
            return false; // el endpoint interpreta esto como 409

        await _repo.Eliminar(producto);
        return true;
    }
}