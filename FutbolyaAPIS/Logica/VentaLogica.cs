using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IVentaLogica
{
    Task<IEnumerable<VentaDto>> ObtenerTodos();
    Task<VentaDto?> ObtenerPorId(int id);
    Task<(VentaDto? resultado, string? error)> Crear(VentaCreateDto dto);
    Task<(bool eliminado, string? error)> Eliminar(int id);
}

public class VentaLogica : IVentaLogica
{
    private readonly IVentaRepository _repo;
    private readonly IVentaDetalladaRepository _repoDetalle;
    private readonly IProductoRepository _repoProducto;

    public VentaLogica(
        IVentaRepository repo,
        IVentaDetalladaRepository repoDetalle,
        IProductoRepository repoProducto)
    {
        _repo          = repo;
        _repoDetalle   = repoDetalle;
        _repoProducto  = repoProducto;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static VentaDto MapDto(Venta v, bool conDetalle = false) =>
        new VentaDto(
            v.Cod_Venta,
            v.Fecha,
            v.Hora,
            v.MontoTotal,
            v.Cod_Usuario,
            $"{v.Usuario?.Nombre} {v.Usuario?.Apellido}",
            conDetalle
                ? v.VentasDetalladas?.Select(vd => new VentaDetalladaDto(
                    vd.Cod_Venta_Detallada,
                    vd.Cod_Producto,
                    vd.Producto?.Nombre ?? string.Empty,
                    vd.Cantidad,
                    vd.Precio,
                    vd.SubTotal))
                : null
        );

    public async Task<IEnumerable<VentaDto>> ObtenerTodos()
    {
        var ventas = await _repo.ObtenerTodos();
        return ventas.Select(v => MapDto(v));
    }

    public async Task<VentaDto?> ObtenerPorId(int id)
    {
        var v = await _repo.ObtenerPorId(id);
        if (v == null) return null;
        return MapDto(v, conDetalle: true);
    }

    public async Task<(VentaDto? resultado, string? error)> Crear(VentaCreateDto dto)
    {
        // Verificar stock de cada producto antes de crear
        foreach (var item in dto.Detalle)
        {
            var producto = await _repoProducto.ObtenerPorId(item.Cod_Producto);
            if (producto == null)
                return (null, $"NOT_FOUND: Producto con id {item.Cod_Producto} no encontrado");

            if (producto.Cantidad < item.Cantidad)
                return (null, $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Cantidad}");
        }

        // Fecha y hora las asigna el servidor
        var venta = new Venta
        {
            Fecha       = DateTime.Today,
            Hora        = DateTime.Now.TimeOfDay,
            Cod_Usuario = dto.Cod_Usuario,
            MontoTotal  = 0
        };

        await _repo.Agregar(venta);

        decimal montoTotal = 0;

        // Crear detalle, descontar stock y calcular totales
        foreach (var item in dto.Detalle)
        {
            var producto = await _repoProducto.ObtenerPorId(item.Cod_Producto);

            var subtotal = producto!.Precio * item.Cantidad;
            montoTotal  += subtotal;

            var detalle = new VentaDetallada
            {
                Cod_Venta    = venta.Cod_Venta,
                Cod_Producto = item.Cod_Producto,
                Cantidad     = item.Cantidad,
                Precio       = producto.Precio,
                SubTotal     = subtotal
            };

            await _repoDetalle.Agregar(detalle);

            // Descontar stock
            producto.Cantidad -= item.Cantidad;
            await _repoProducto.Actualizar(producto);
        }

        // Actualizar monto total de la venta
        venta.MontoTotal = montoTotal;
        await _repo.Actualizar(venta);

        var creada = await _repo.ObtenerPorId(venta.Cod_Venta);
        return (MapDto(creada!, conDetalle: true), null);
    }

    public async Task<(bool eliminado, string? error)> Eliminar(int id)
    {
        var venta = await _repo.ObtenerPorId(id);
        if (venta == null)
            return (false, "NOT_FOUND");

        // Restituir stock de cada producto al anular
        foreach (var detalle in venta.VentasDetalladas ?? Enumerable.Empty<VentaDetallada>())
        {
            var producto = await _repoProducto.ObtenerPorId(detalle.Cod_Producto);
            if (producto != null)
            {
                producto.Cantidad += detalle.Cantidad;
                await _repoProducto.Actualizar(producto);
            }
        }

        await _repo.Eliminar(venta);
        return (true, null);
    }
}