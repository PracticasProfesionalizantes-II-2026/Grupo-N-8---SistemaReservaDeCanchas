using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Logica.Excepciones;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IVentaLogica
{
    Task<IEnumerable<VentaDto>> ObtenerTodos(bool incluirBajas = false);
    Task<VentaDto?> ObtenerPorId(int id);
    Task<(VentaDto? resultado, string? error)> Crear(VentaCreateDto dto, int codUsuarioAccion);
    Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion);
    Task<(VentaDto? resultado, string? error)> Reactivar(int id, int codUsuarioAccion);
}

// Encapsula las reglas de negocio de ventas (validaciones, control de stock, registro de
// auditoría) entre los Endpoints y los Repositorios. Las operaciones de escritura (Crear,
// Eliminar, Reactivar) corren dentro de una transacción de IUnidadDeTrabajo para mantener
// Venta y VentaDetallada consistentes.
public class VentaLogica : IVentaLogica
{
    private readonly IVentaRepository _repo;
    private readonly IVentaDetalladaRepository _repoDetalle;
    private readonly IProductoRepository _repoProducto;
    private readonly IAuditoriaLogica _auditoria;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public VentaLogica(
        IVentaRepository repo,
        IVentaDetalladaRepository repoDetalle,
        IProductoRepository repoProducto,
        IAuditoriaLogica auditoria,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _repo            = repo;
        _repoDetalle     = repoDetalle;
        _repoProducto    = repoProducto;
        _auditoria       = auditoria;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static VentaDto MapDto(Venta v, bool conDetalle = false) =>
        new(
            v.Cod_Venta,
            v.Fecha,
            v.Hora,
            v.MontoTotal,
            v.Cod_Usuario,
            $"{v.Usuario?.Nombre} {v.Usuario?.Apellido}",
            v.Activo,
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

    public async Task<IEnumerable<VentaDto>> ObtenerTodos(bool incluirBajas = false)
    {
        var ventas = await _repo.ObtenerTodos();
        if (!incluirBajas)
            ventas = ventas.Where(v => v.Activo);

        return ventas.Select(v => MapDto(v));
    }

    public async Task<VentaDto?> ObtenerPorId(int id)
    {
        var v = await _repo.ObtenerPorId(id);
        if (v == null) return null;
        return MapDto(v, conDetalle: true);
    }

    // Crear hace varias escrituras (Venta + VentaDetallada por producto + descuento de stock)
    // dentro de una sola transacción: si el descuento atómico de stock pierde la carrera contra
    // otra venta concurrente (0 filas afectadas), se aborta con OperacionInvalidaException y se
    // hace rollback de todo lo anterior en esta operación.
    public async Task<(VentaDto? resultado, string? error)> Crear(VentaCreateDto dto, int codUsuarioAccion)
    {
        var detalleLista = dto.Detalle?.ToList() ?? new List<VentaDetalladaItemDto>();
        if (detalleLista.Count == 0)
            return (null, "Debe ingresar un producto");

        if (detalleLista.Any(d => d.Cantidad <= 0))
            return (null, "La cantidad de cada producto debe ser mayor a 0");

        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var productosAgrupados = detalleLista
                    .GroupBy(d => d.Cod_Producto)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

                foreach (var (cod_Producto, cantidadTotal) in productosAgrupados)
                {
                    var producto = await _repoProducto.ObtenerPorId(cod_Producto);
                    if (producto == null || !producto.Activo)
                        throw new OperacionInvalidaException($"NOT_FOUND: Producto con id {cod_Producto} no encontrado");

                    if (producto.Cantidad < cantidadTotal)
                        throw new OperacionInvalidaException($"INSUFFICIENT_STOCK: Producto {producto.Nombre} tiene stock insuficiente. Disponible: {producto.Cantidad}, requerido: {cantidadTotal}");
                }

                // Cod_Usuario se toma del JWT autenticado, no del cuerpo enviado por el cliente,
                // para que no pueda falsificarse.
                var venta = new Venta
                {
                    Fecha       = DateTime.Today,
                    Hora        = DateTime.Now.TimeOfDay,
                    Cod_Usuario = codUsuarioAccion,
                    MontoTotal  = 0,
                    Activo      = true
                };

                await _repo.Agregar(venta);

                decimal montoTotal = 0;

                foreach (var (cod_Producto, cantidadTotal) in productosAgrupados)
                {
                    var producto = await _repoProducto.ObtenerPorId(cod_Producto);
                    var subTotal = producto!.Precio * cantidadTotal;
                    montoTotal += subTotal;

                    await _repoDetalle.Agregar(new VentaDetallada
                    {
                        Cod_Venta    = venta.Cod_Venta,
                        Cod_Producto = cod_Producto,
                        Cantidad     = cantidadTotal,
                        Precio       = producto.Precio,
                        SubTotal     = subTotal
                    });

                    // Update atómico condicional: 0 filas afectadas significa que otra venta o
                    // reactivación concurrente ya se llevó el stock disponible entre la verificación
                    // de arriba y este punto. No volver a llamar _repoProducto.Actualizar(producto)
                    // acá: pisaría este update con la Cantidad vieja que ese objeto tiene en memoria.
                    var descontado = await _repoProducto.DescontarStock(cod_Producto, cantidadTotal);
                    if (!descontado)
                        throw new OperacionInvalidaException($"INSUFFICIENT_STOCK: Producto {producto.Nombre} tiene stock insuficiente. Disponible: {producto.Cantidad}, requerido: {cantidadTotal}");
                }

                venta.MontoTotal = montoTotal;
                await _repo.Actualizar(venta);

                var creada = await _repo.ObtenerPorId(venta.Cod_Venta);
                var resultado = MapDto(creada!, conDetalle: true);
                await _auditoria.Registrar(codUsuarioAccion, "Venta", venta.Cod_Venta, AccionAuditoria.Alta, null, resultado);
                return (resultado, (string?)null);
            });
        }
        catch (OperacionInvalidaException ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var venta = await _repo.ObtenerPorId(id);
                if (venta == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                if (!venta.Activo)
                    return (false, "La venta ya estaba anulada");

                var anterior = MapDto(venta, conDetalle: true);

                // Restituir stock de cada producto al anular (incremento atómico); se conserva
                // el detalle como historial.
                foreach (var detalle in venta.VentasDetalladas ?? Enumerable.Empty<VentaDetallada>())
                {
                    await _repoProducto.RestaurarStock(detalle.Cod_Producto, detalle.Cantidad);
                }

                venta.Activo = false;
                await _repo.Actualizar(venta);

                await _auditoria.Registrar(codUsuarioAccion, "Venta", id, AccionAuditoria.Baja, anterior, null);
                return (true, (string?)null);
            });
        }
        catch (OperacionInvalidaException ex)
        {
            return (false, ex.Message);
        }
    }

    // Reactivar vuelve a descontar el stock que se había restituido al anular, así que puede
    // fallar si ese stock ya se usó en otra venta o reserva mientras tanto. La verificación y el
    // descuento corren dentro de una transacción con updates atómicos, así que la carrera contra
    // otra venta concurrente se detecta de forma confiable (0 filas afectadas) en vez de basarse
    // en un valor de Cantidad leído momentos antes.
    public async Task<(VentaDto? resultado, string? error)> Reactivar(int id, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var venta = await _repo.ObtenerPorId(id);
                if (venta == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                if (venta.Activo)
                    throw new OperacionInvalidaException("La venta ya está activa");

                var detalle = (venta.VentasDetalladas ?? Enumerable.Empty<VentaDetallada>()).ToList();

                foreach (var d in detalle)
                {
                    var producto = await _repoProducto.ObtenerPorId(d.Cod_Producto);
                    if (producto == null || !producto.Activo)
                        throw new OperacionInvalidaException("El producto de esta venta ya no está disponible; no se puede reactivar");
                    if (producto.Cantidad < d.Cantidad)
                        throw new OperacionInvalidaException($"Stock insuficiente para reactivar: '{producto.Nombre}' tiene {producto.Cantidad} disponible, se necesitan {d.Cantidad}");
                }

                var anterior = MapDto(venta, conDetalle: true);

                foreach (var d in detalle)
                {
                    var descontado = await _repoProducto.DescontarStock(d.Cod_Producto, d.Cantidad);
                    if (!descontado)
                        throw new OperacionInvalidaException("Stock insuficiente para reactivar: otra operación se llevó el stock disponible");
                }

                venta.Activo = true;
                await _repo.Actualizar(venta);

                var resultado = MapDto(venta, conDetalle: true);
                await _auditoria.Registrar(codUsuarioAccion, "Venta", id, AccionAuditoria.Modificacion, anterior, resultado);
                return (resultado, (string?)null);
            });
        }
        catch (OperacionInvalidaException ex)
        {
            return (null, ex.Message);
        }
    }
}
