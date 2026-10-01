using System.Text.RegularExpressions;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Logica.Excepciones;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IProductoLogica
{
    Task<IEnumerable<ProductoDto>> ObtenerTodos(bool incluirBajas = false);
    Task<ProductoDto?> ObtenerPorId(int id);
    Task<(ProductoDto? resultado, string? error)> Crear(ProductoCreateDto dto, int codUsuarioAccion);
    Task<(ProductoDto? resultado, string? error)> Actualizar(int id, ProductoUpdateDto dto, int codUsuarioAccion);
    Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion);
    Task<(ProductoDto? resultado, string? error)> Reactivar(int id, int codUsuarioAccion);

    // Ajuste relativo de stock (+n ingreso / -n egreso), atómico y auditado; reemplaza la
    // edición de Cantidad vía el PUT completo (ver ProductoUpdateDto).
    Task<(ProductoDto? resultado, string? error)> AjustarStock(int id, int ajuste, int codUsuarioAccion);
}

// Reglas de negocio de producto (bebidas/comidas): validación de campos, chequeo de
// duplicados, ajuste de stock y auditoría. Se ubica entre los Endpoints y ProductoRepository.
public class ProductoLogica : IProductoLogica
{
    private static readonly string[] TiposValidos = { "Bebida", "Comida" };

    // Regla de negocio para el nombre; Precio ya es decimal(10,2) en el modelo, así que sin
    // este tope EF tira una excepción al guardar.
    private const int NombreMaxLength = 100;
    private const decimal PrecioMax = 99_999_999.99m;

    // Tope superior simple para un ajuste de stock (no hay regla de negocio real para un
    // máximo; esto es solo para no dejar que un ajuste mal tipeado deje un stock absurdo o
    // que se acerque a un desborde de int).
    private const int StockMax = 1_000_000;

    private static readonly Regex RegexEspacios = new(@"\s+", RegexOptions.Compiled);

    private readonly IProductoRepository _repo;
    private readonly IAuditoriaLogica _auditoria;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public ProductoLogica(IProductoRepository repo, IAuditoriaLogica auditoria, IUnidadDeTrabajo unidadDeTrabajo)
    {
        _repo            = repo;
        _auditoria       = auditoria;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    private static ProductoDto MapDto(Producto p) =>
        new(p.Cod_Producto, p.Nombre, p.Cantidad, p.Precio, p.Tipo, p.Activo);

    // Recorta y colapsa espacios internos, para que "gaseosa   cola" y "gaseosa cola" se
    // detecten como el mismo nombre al chequear duplicados.
    private static string NormalizarNombre(string nombre) => RegexEspacios.Replace(nombre.Trim(), " ");

    // El nombre tampoco puede repetir el de un producto dado de baja: ese producto se reactiva,
    // no se vuelve a crear, para no dejar duplicados en la base.
    private static string? ValidarNombreUnico(IEnumerable<Producto> productos, string nombre, int? codExcluido = null)
    {
        var existente = productos.FirstOrDefault(p => p.Cod_Producto != codExcluido
                                                   && p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        if (existente is null)
            return null;

        return existente.Activo
            ? "Ya existe un producto con ese nombre"
            : "Ya existe un producto dado de baja con ese nombre. Reactivalo o usá otro nombre.";
    }

    private static string? ValidarCamposComunes(string nombreNormalizado, decimal precio, string tipo)
    {
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
            return "El nombre es obligatorio";
        if (nombreNormalizado.Length > NombreMaxLength)
            return $"El nombre no puede superar los {NombreMaxLength} caracteres";
        if (precio < 0)
            return "El precio no puede ser negativo";
        if (precio > PrecioMax)
            return $"El precio no puede superar {PrecioMax:N2}";
        if (!TiposValidos.Contains(tipo, StringComparer.OrdinalIgnoreCase))
            return "El tipo debe ser 'Bebida' o 'Comida'";
        return null;
    }

    private static string? ValidarCampos(ProductoCreateDto dto, string nombreNormalizado)
    {
        var errorComun = ValidarCamposComunes(nombreNormalizado, dto.Precio, dto.Tipo);
        if (errorComun != null)
            return errorComun;
        if (dto.Cantidad < 0)
            return "La cantidad no puede ser negativa";
        return null;
    }

    public async Task<IEnumerable<ProductoDto>> ObtenerTodos(bool incluirBajas = false)
    {
        var productos = await _repo.ObtenerTodos();
        if (!incluirBajas)
            productos = productos.Where(p => p.Activo);

        return productos.Select(MapDto);
    }

    public async Task<ProductoDto?> ObtenerPorId(int id)
    {
        var p = await _repo.ObtenerPorId(id);
        return p == null ? null : MapDto(p);
    }

    public async Task<(ProductoDto? resultado, string? error)> Crear(ProductoCreateDto dto, int codUsuarioAccion)
    {
        var nombreNormalizado = NormalizarNombre(dto.Nombre);
        var errorValidacion = ValidarCampos(dto, nombreNormalizado);
        if (errorValidacion != null)
            return (null, errorValidacion);

        var productos = await _repo.ObtenerTodos();
        var errorNombre = ValidarNombreUnico(productos, nombreNormalizado);
        if (errorNombre != null)
            return (null, errorNombre);

        var producto = new Producto
        {
            Nombre   = nombreNormalizado,
            Cantidad = dto.Cantidad,
            Precio   = dto.Precio,
            Tipo     = dto.Tipo.Trim(),
            Activo   = true
        };
        await _repo.Agregar(producto);

        var resultado = MapDto(producto);
        await _auditoria.Registrar(codUsuarioAccion, "Producto", producto.Cod_Producto, AccionAuditoria.Alta, null, resultado);
        return (resultado, null);
    }

    // No recibe/edita Cantidad (ver ProductoUpdateDto): el stock se ajusta solo vía
    // AjustarStock, para no pisar ventas concurrentes con un valor stale del formulario
    // ("lost update" al guardar una edición abierta antes de una venta que ya cambió el stock).
    public async Task<(ProductoDto? resultado, string? error)> Actualizar(int id, ProductoUpdateDto dto, int codUsuarioAccion)
    {
        var nombreNormalizado = NormalizarNombre(dto.Nombre);
        var errorValidacion = ValidarCamposComunes(nombreNormalizado, dto.Precio, dto.Tipo);
        if (errorValidacion != null)
            return (null, errorValidacion);

        var productos = await _repo.ObtenerTodos();
        var errorNombre = ValidarNombreUnico(productos, nombreNormalizado, id);
        if (errorNombre != null)
            return (null, errorNombre);

        var producto = await _repo.ObtenerPorId(id);
        if (producto == null)
            return (null, "NOT_FOUND");

        var anterior = MapDto(producto);
        producto.Nombre = nombreNormalizado;
        producto.Precio = dto.Precio;
        producto.Tipo   = dto.Tipo.Trim();

        await _repo.Actualizar(producto);
        var resultado = MapDto(producto);
        await _auditoria.Registrar(codUsuarioAccion, "Producto", id, AccionAuditoria.Modificacion, anterior, resultado);
        return (resultado, null);
    }

    // Ajuste relativo y atómico de stock. Positivo = ingreso (RestaurarStock, incremento
    // incondicional); negativo = egreso (DescontarStock, update condicional WHERE Cantidad
    // >= n). Corre en una transacción para que el update atómico + el registro de auditoría
    // (con el valor anterior/nuevo) sean una sola unidad. Ver el comentario en
    // ProductoRepository.DescontarStock: no se vuelve a llamar _repo.Actualizar(producto) con
    // el objeto ya cargado después del update atómico.
    public async Task<(ProductoDto? resultado, string? error)> AjustarStock(int id, int ajuste, int codUsuarioAccion)
    {
        if (ajuste == 0)
            return (null, "El ajuste debe ser distinto de 0 (positivo para ingreso, negativo para egreso)");

        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var producto = await _repo.ObtenerPorId(id);
                if (producto == null || !producto.Activo)
                    throw new OperacionInvalidaException("NOT_FOUND");

                var anterior = MapDto(producto);

                if (ajuste > 0)
                {
                    // Aritmética en long para no desbordar int antes de comparar contra StockMax.
                    long stockResultante = (long)producto.Cantidad + ajuste;
                    if (stockResultante > StockMax)
                        throw new OperacionInvalidaException($"El ajuste dejaría un stock de {stockResultante}, que supera el máximo permitido ({StockMax})");

                    await _repo.RestaurarStock(id, ajuste);
                }
                else
                {
                    var cantidadADescontar = -ajuste;
                    var descontado = await _repo.DescontarStock(id, cantidadADescontar);
                    if (!descontado)
                    {
                        // Se relee después del update fallido para informar el stock real (no el
                        // valor de `producto` en memoria, que puede haber quedado desactualizado).
                        var actual = await _repo.ObtenerPorId(id);
                        throw new OperacionInvalidaException($"No se puede descontar {cantidadADescontar}: stock disponible {actual?.Cantidad ?? 0}");
                    }
                }

                var actualizado = await _repo.ObtenerPorId(id);
                var resultado = MapDto(actualizado!);
                await _auditoria.Registrar(codUsuarioAccion, "Producto", id, AccionAuditoria.Modificacion, anterior, resultado);
                return (resultado, (string?)null);
            });
        }
        catch (OperacionInvalidaException ex)
        {
            return (null, ex.Message);
        }
    }

    // A diferencia de MaterialDeportivoLogica.Eliminar, este método no consulta ninguna otra
    // tabla relacionada (no hay chequeo de "reservas pendientes" para un Producto) — solo lee
    // y marca de baja la misma fila. No hay nada concurrency-relevant que envolver acá en una
    // transacción Serializable.
    public async Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion)
    {
        var producto = await _repo.ObtenerPorId(id);
        if (producto == null)
            return (false, "NOT_FOUND");

        if (!producto.Activo)
            return (false, "El producto ya está dado de baja");

        var anterior = MapDto(producto);
        producto.Activo = false;
        await _repo.Actualizar(producto);

        await _auditoria.Registrar(codUsuarioAccion, "Producto", id, AccionAuditoria.Baja, anterior, null);
        return (true, null);
    }

    public async Task<(ProductoDto? resultado, string? error)> Reactivar(int id, int codUsuarioAccion)
    {
        var producto = await _repo.ObtenerPorId(id);
        if (producto == null)
            return (null, "NOT_FOUND");

        if (producto.Activo)
            return (null, "El producto ya está activo");

        var productos = await _repo.ObtenerTodos();
        if (productos.Any(p => p.Activo && p.Nombre.Equals(producto.Nombre, StringComparison.OrdinalIgnoreCase) && p.Cod_Producto != id))
            return (null, "Ya existe otro producto activo con ese nombre; no se puede reactivar");

        var anterior = MapDto(producto);
        producto.Activo = true;
        await _repo.Actualizar(producto);

        var resultado = MapDto(producto);
        await _auditoria.Registrar(codUsuarioAccion, "Producto", id, AccionAuditoria.Modificacion, anterior, resultado);
        return (resultado, null);
    }
}
