using System.Data;
using System.Text.RegularExpressions;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Logica.Excepciones;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IMaterialDeportivoLogica
{
    Task<IEnumerable<MaterialDeportivoDto>> ObtenerTodos(bool incluirBajas = false);
    Task<MaterialDeportivoDto?> ObtenerPorId(int id);
    Task<(MaterialDeportivoDto? resultado, string? error)> Crear(MaterialDeportivoCreateDto dto, int codUsuarioAccion);
    Task<(MaterialDeportivoDto? resultado, string? error)> Actualizar(int id, MaterialDeportivoUpdateDto dto, int codUsuarioAccion);
    Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion);
    Task<(MaterialDeportivoDto? resultado, string? error)> Reactivar(int id, int codUsuarioAccion);

    // Ajuste relativo de stock (+n ingreso / -n egreso); ver comentario equivalente en IProductoLogica.AjustarStock.
    Task<(MaterialDeportivoDto? resultado, string? error)> AjustarStock(int id, int ajuste, int codUsuarioAccion);
}

// Reglas de negocio de material deportivo: validación de nombre, chequeo de duplicados,
// ajuste de stock y auditoría. Se ubica entre los Endpoints y MaterialDeportivoRepository.
public class MaterialDeportivoLogica : IMaterialDeportivoLogica
{
    // El nombre no puede superar los 100 caracteres; sin este tope, EF tira una excepción
    // sin manejar al guardar.
    private const int NombreMaxLength = 100;

    // Tope superior simple para un ajuste de stock; ver comentario equivalente en ProductoLogica.
    private const int StockMax = 1_000_000;

    private static readonly Regex RegexEspacios = new(@"\s+", RegexOptions.Compiled);

    private readonly IMaterialDeportivoRepository _repo;
    private readonly IReservaMaterialRepository _repoReservaMaterial;
    private readonly IAuditoriaLogica _auditoria;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public MaterialDeportivoLogica(
        IMaterialDeportivoRepository repo,
        IReservaMaterialRepository repoReservaMaterial,
        IAuditoriaLogica auditoria,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _repo                = repo;
        _repoReservaMaterial = repoReservaMaterial;
        _auditoria           = auditoria;
        _unidadDeTrabajo     = unidadDeTrabajo;
    }

    private static MaterialDeportivoDto MapDto(Material_Deportivo m) =>
        new(m.Cod_Material, m.Nombre, m.Cant_Material, m.Activo);

    // Recorta y colapsa espacios internos, para que "pelota   futbol" y "pelota futbol" se
    // detecten como el mismo nombre al chequear duplicados.
    private static string NormalizarNombre(string nombre) => RegexEspacios.Replace(nombre.Trim(), " ");

    // El nombre tampoco puede repetir el de un material dado de baja: ese material se reactiva,
    // no se vuelve a crear, para no dejar duplicados en la base.
    private static string? ValidarNombreUnico(IEnumerable<Material_Deportivo> materiales, string nombre, int? codExcluido = null)
    {
        var existente = materiales.FirstOrDefault(m => m.Cod_Material != codExcluido
                                                    && m.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        if (existente is null)
            return null;

        return existente.Activo
            ? "Ya existe un material con ese nombre"
            : "Ya existe un material dado de baja con ese nombre. Reactivalo o usá otro nombre.";
    }

    public async Task<IEnumerable<MaterialDeportivoDto>> ObtenerTodos(bool incluirBajas = false)
    {
        var materiales = await _repo.ObtenerTodos();
        if (!incluirBajas)
            materiales = materiales.Where(m => m.Activo);

        return materiales.Select(MapDto);
    }

    public async Task<MaterialDeportivoDto?> ObtenerPorId(int id)
    {
        var m = await _repo.ObtenerPorId(id);
        return m == null ? null : MapDto(m);
    }

    public async Task<(MaterialDeportivoDto? resultado, string? error)> Crear(MaterialDeportivoCreateDto dto, int codUsuarioAccion)
    {
        var nombreNormalizado = NormalizarNombre(dto.Nombre);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
            return (null, "El nombre es obligatorio");
        if (nombreNormalizado.Length > NombreMaxLength)
            return (null, $"El nombre no puede superar los {NombreMaxLength} caracteres");

        if (dto.Cant_Material < 0)
            return (null, "La cantidad no puede ser negativa");

        var materiales = await _repo.ObtenerTodos();
        var errorNombre = ValidarNombreUnico(materiales, nombreNormalizado);
        if (errorNombre != null)
            return (null, errorNombre);

        var material = new Material_Deportivo
        {
            Nombre        = nombreNormalizado,
            Cant_Material = dto.Cant_Material,
            Activo        = true
        };

        await _repo.Agregar(material);
        var resultado = MapDto(material);
        await _auditoria.Registrar(codUsuarioAccion, "Material_Deportivo", material.Cod_Material, AccionAuditoria.Alta, null, resultado);
        return (resultado, null);
    }

    // No recibe/edita Cant_Material (ver MaterialDeportivoUpdateDto): el stock se ajusta solo
    // vía AjustarStock. Mismo motivo que ProductoLogica.Actualizar.
    public async Task<(MaterialDeportivoDto? resultado, string? error)> Actualizar(int id, MaterialDeportivoUpdateDto dto, int codUsuarioAccion)
    {
        var nombreNormalizado = NormalizarNombre(dto.Nombre);
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
            return (null, "El nombre es obligatorio");
        if (nombreNormalizado.Length > NombreMaxLength)
            return (null, $"El nombre no puede superar los {NombreMaxLength} caracteres");

        var materiales = await _repo.ObtenerTodos();
        var errorNombre = ValidarNombreUnico(materiales, nombreNormalizado, id);
        if (errorNombre != null)
            return (null, errorNombre);

        var material = await _repo.ObtenerPorId(id);
        if (material == null)
            return (null, "NOT_FOUND");

        var anterior = MapDto(material);
        material.Nombre = nombreNormalizado;

        await _repo.Actualizar(material);
        var resultado = MapDto(material);
        await _auditoria.Registrar(codUsuarioAccion, "Material_Deportivo", id, AccionAuditoria.Modificacion, anterior, resultado);
        return (resultado, null);
    }

    // La verificación de reservas pendientes + la baja corren en una sola transacción
    // Serializable, igual que ReservaLogica.Crear. Bajo Serializable, la lectura de
    // _repoReservaMaterial.ObtenerTodos() (que trae todo Reserva_Material con su Reserva) deja
    // rangeados esos datos hasta el commit: si una Reserva.Crear concurrente inserta una
    // Reserva_Material para este material mientras esta transacción está en curso, SQL Server
    // detecta el conflicto de serialización (1205/3960) y UnidadDeTrabajo reintenta, evitando
    // el TOCTOU de "reservas pendientes: 0" seguido de una reserva nueva que igual queda huérfana.
    // ProductoLogica.Eliminar no necesita este tratamiento: ninguna otra transacción concurrente
    // lee filas de Producto, así que no hay nada concurrency-relevant que envolver ahí.
    public async Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var material = await _repo.ObtenerPorId(id);
                if (material == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                if (!material.Activo)
                    return (false, "El material ya está dado de baja");

                // No se puede dar de baja un material con reservas pendientes que lo usan; el
                // historial de reservas ya realizadas o canceladas no se ve afectado.
                var reservasMateriales = await _repoReservaMaterial.ObtenerTodos();
                var reservasPendientes = reservasMateriales.Count(rm =>
                    rm.Cod_Material == id &&
                    rm.Reserva.Estado == EstadoReserva.Pendiente);

                if (reservasPendientes > 0)
                    return (false, $"No se puede dar de baja: está en {reservasPendientes} reserva(s) pendiente(s). Esperá a que se realicen o cancelalas primero.");

                // Baja lógica: se conserva el historial de reservas asociadas.
                var anterior = MapDto(material);
                material.Activo = false;
                await _repo.Actualizar(material);

                await _auditoria.Registrar(codUsuarioAccion, "Material_Deportivo", id, AccionAuditoria.Baja, anterior, null);
                return (true, (string?)null);
            }, IsolationLevel.Serializable);
        }
        catch (OperacionInvalidaException ex)
        {
            return (false, ex.Message);
        }
    }

    // Ajuste relativo y atómico de stock; ver comentario equivalente en ProductoLogica.AjustarStock.
    public async Task<(MaterialDeportivoDto? resultado, string? error)> AjustarStock(int id, int ajuste, int codUsuarioAccion)
    {
        if (ajuste == 0)
            return (null, "El ajuste debe ser distinto de 0 (positivo para ingreso, negativo para egreso)");

        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var material = await _repo.ObtenerPorId(id);
                if (material == null || !material.Activo)
                    throw new OperacionInvalidaException("NOT_FOUND");

                var anterior = MapDto(material);

                if (ajuste > 0)
                {
                    long stockResultante = (long)material.Cant_Material + ajuste;
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
                        var actual = await _repo.ObtenerPorId(id);
                        throw new OperacionInvalidaException($"No se puede descontar {cantidadADescontar}: stock disponible {actual?.Cant_Material ?? 0}");
                    }
                }

                var actualizado = await _repo.ObtenerPorId(id);
                var resultado = MapDto(actualizado!);
                await _auditoria.Registrar(codUsuarioAccion, "Material_Deportivo", id, AccionAuditoria.Modificacion, anterior, resultado);
                return (resultado, (string?)null);
            });
        }
        catch (OperacionInvalidaException ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(MaterialDeportivoDto? resultado, string? error)> Reactivar(int id, int codUsuarioAccion)
    {
        var material = await _repo.ObtenerPorId(id);
        if (material == null)
            return (null, "NOT_FOUND");

        if (material.Activo)
            return (null, "El material ya está activo");

        var materiales = await _repo.ObtenerTodos();
        if (materiales.Any(m => m.Activo && m.Nombre.Equals(material.Nombre, StringComparison.OrdinalIgnoreCase) && m.Cod_Material != id))
            return (null, "Ya existe otro material activo con ese nombre; no se puede reactivar");

        var anterior = MapDto(material);
        material.Activo = true;
        await _repo.Actualizar(material);

        var resultado = MapDto(material);
        await _auditoria.Registrar(codUsuarioAccion, "Material_Deportivo", id, AccionAuditoria.Modificacion, anterior, resultado);
        return (resultado, null);
    }
}
