using System.Data;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Logica.Excepciones;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface ICanchaLogica
{
    Task<IEnumerable<CanchaDto>> ObtenerTodos(bool incluirBajas = false);
    Task<CanchaDto?> ObtenerPorId(int id);
    Task<(int? codCancha, string? error)> Crear(CanchaCreateDto dto, int codUsuarioAccion);
    Task<bool> ActualizarDescripcion(int id, string descripcion, int codUsuarioAccion);
    Task<(bool actualizado, string? error)> ActualizarEstado(int id, EstadoCancha estado, int codUsuarioAccion);
    Task<(bool actualizado, string? error)> ActualizarHorarios(int id, List<int> codHorarios, int codUsuarioAccion);
    Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion);
}

// Encapsula las reglas de negocio de canchas (validación de horarios, control de reservas
// pendientes, registro de auditoría) entre los Endpoints y los Repositorios. Los cambios de
// estado, horarios y baja corren en transacciones Serializable para evitar condiciones de
// carrera con la creación de reservas.
public class CanchaLogica : ICanchaLogica
{
    private readonly ICanchaRepository _repo;
    private readonly ICanchaHorarioRepository _repoCanchaHorario;
    private readonly IHorarioDisponibleRepository _repoHorario;
    private readonly IReservaRepository _repoReserva;
    private readonly IAuditoriaLogica _auditoria;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public CanchaLogica(
        ICanchaRepository repo,
        ICanchaHorarioRepository repoCanchaHorario,
        IHorarioDisponibleRepository repoHorario,
        IReservaRepository repoReserva,
        IAuditoriaLogica auditoria,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _repo               = repo;
        _repoCanchaHorario  = repoCanchaHorario;
        _repoHorario        = repoHorario;
        _repoReserva        = repoReserva;
        _auditoria          = auditoria;
        _unidadDeTrabajo    = unidadDeTrabajo;
    }

    private static CanchaDto MapDto(Cancha c) => new(
        c.Cod_Cancha,
        c.Nombre,
        c.Descripcion,
        c.Estado.ToString(),
        c.CanchaHorarios.Select(ch => ch.Cod_Horario).OrderBy(id => id).ToList()
    );

    private async Task<string?> ValidarCodHorarios(List<int> codHorarios)
    {
        if (codHorarios.Count == 0)
            return null;

        var catalogo = (await _repoHorario.ObtenerTodos()).ToDictionary(h => h.Cod_Horario);
        foreach (var id in codHorarios.Distinct())
        {
            if (!catalogo.TryGetValue(id, out var horario))
                return $"El horario con id {id} no existe en el catálogo";
            if (!horario.Activo)
                return $"El horario con id {id} no está activo";
        }

        return null;
    }

    public async Task<IEnumerable<CanchaDto>> ObtenerTodos(bool incluirBajas = false)
    {
        var canchas = await _repo.ObtenerTodos();
        if (!incluirBajas)
            canchas = canchas.Where(c => c.Estado != EstadoCancha.Baja);

        return canchas.Select(MapDto);
    }

    public async Task<CanchaDto?> ObtenerPorId(int id)
    {
        var c = await _repo.ObtenerPorId(id);
        return c == null ? null : MapDto(c);
    }

    public async Task<(int? codCancha, string? error)> Crear(CanchaCreateDto dto, int codUsuarioAccion)
    {
        var errorHorarios = await ValidarCodHorarios(dto.Cod_Horarios);
        if (errorHorarios != null)
            return (null, errorHorarios);

        var cancha = new Cancha
        {
            Descripcion = dto.Descripcion,
            Nombre      = "Cancha N° 0",
            Estado      = EstadoCancha.Disponible
        };

        await _repo.Agregar(cancha);

        cancha.Nombre = $"Cancha N° {cancha.Cod_Cancha}";
        await _repo.Actualizar(cancha);

        if (dto.Cod_Horarios.Count > 0)
            await _repoCanchaHorario.Reemplazar(cancha.Cod_Cancha, dto.Cod_Horarios);

        var creada = await _repo.ObtenerPorId(cancha.Cod_Cancha);
        await _auditoria.Registrar(codUsuarioAccion, "Cancha", cancha.Cod_Cancha, AccionAuditoria.Alta, null, MapDto(creada!));

        return (cancha.Cod_Cancha, null);
    }

    public async Task<bool> ActualizarDescripcion(int id, string descripcion, int codUsuarioAccion)
    {
        var cancha = await _repo.ObtenerPorId(id);
        if (cancha == null) return false;

        var anterior = MapDto(cancha);
        cancha.Descripcion = descripcion;

        await _repo.Actualizar(cancha);
        await _auditoria.Registrar(codUsuarioAccion, "Cancha", id, AccionAuditoria.Modificacion, anterior, MapDto(cancha));
        return true;
    }

    // Cambia el estado de una cancha (Disponible/Mantenimiento/Baja). La verificación de reservas
    // pendientes y el cambio de estado corren en una transacción Serializable: ReservaLogica.Crear
    // también corre en Serializable y lee la misma Cancha y sus Reserva_Horario dentro de su
    // propia transacción, así que ambas transacciones range-lockean las mismas filas y SQL Server
    // detecta el conflicto (1205/3960, reintentado por UnidadDeTrabajo) en vez de dejar una
    // reserva nueva colada justo cuando esta cancha pasa a Mantenimiento/Baja, o viceversa.
    public async Task<(bool actualizado, string? error)> ActualizarEstado(int id, EstadoCancha estado, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var cancha = await _repo.ObtenerPorId(id);
                if (cancha == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                // No se permite pasar una cancha a Mantenimiento/Baja con reservas pendientes:
                // quedarían huérfanas.
                if (estado != EstadoCancha.Disponible)
                {
                    var reservas = await _repoReserva.ObtenerTodos();
                    var reservasPendientes = reservas.Count(r => r.Cod_Cancha == id && r.Estado == EstadoReserva.Pendiente);
                    if (reservasPendientes > 0)
                        return (false, $"No se puede cambiar el estado: tiene {reservasPendientes} reserva(s) pendiente(s). Cancelalas primero.");
                }

                var anterior = MapDto(cancha);
                cancha.Estado = estado;

                await _repo.Actualizar(cancha);
                await _auditoria.Registrar(codUsuarioAccion, "Cancha", id, AccionAuditoria.Modificacion, anterior, MapDto(cancha));
                return (true, (string?)null);
            }, IsolationLevel.Serializable);
        }
        catch (OperacionInvalidaException ex)
        {
            return (false, ex.Message);
        }
    }

    // Reemplaza el conjunto de horarios habilitados de una cancha. Corre en la misma transacción
    // Serializable que ActualizarEstado, por el mismo motivo de concurrencia con
    // ReservaLogica.Crear.
    public async Task<(bool actualizado, string? error)> ActualizarHorarios(int id, List<int> codHorarios, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var cancha = await _repo.ObtenerPorId(id);
                if (cancha == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                var errorHorarios = await ValidarCodHorarios(codHorarios);
                if (errorHorarios != null)
                    return (false, errorHorarios);

                // No se puede quitar de la cancha un horario que una reserva Pendiente ya está usando.
                var horariosNuevos = codHorarios.ToHashSet();
                var reservasCancha = await _repoReserva.ObtenerTodos();
                var conflictos = reservasCancha
                    .Where(r => r.Cod_Cancha == id && r.Estado == EstadoReserva.Pendiente)
                    .SelectMany(r => r.ReservaHorarios
                        .Where(rh => !horariosNuevos.Contains(rh.Cod_Horario))
                        .Select(rh => new { Reserva = r, Horario = rh.HorarioDisponible }))
                    .OrderBy(c => c.Reserva.FechaReserva)
                    .ThenBy(c => c.Horario.HoraInicio)
                    .Select(c => $"{c.Horario.HoraInicio:hh\\:mm}-{c.Horario.HoraFin:hh\\:mm} (reserva #{c.Reserva.Cod_Reserva} del {c.Reserva.FechaReserva:dd/MM/yyyy})")
                    .ToList();
                if (conflictos.Count > 0)
                    return (false, $"No se pueden quitar horarios usados por reservas pendientes: {string.Join(", ", conflictos)}. Cancelá esas reservas o dejá esos horarios marcados.");

                var anterior = MapDto(cancha);
                await _repoCanchaHorario.Reemplazar(id, codHorarios);

                var actualizada = await _repo.ObtenerPorId(id);
                await _auditoria.Registrar(codUsuarioAccion, "Cancha", id, AccionAuditoria.Modificacion, anterior, MapDto(actualizada!));
                return (true, (string?)null);
            }, IsolationLevel.Serializable);
        }
        catch (OperacionInvalidaException ex)
        {
            return (false, ex.Message);
        }
    }

    // Da de baja lógica una cancha; corre en la misma transacción Serializable que
    // ActualizarEstado, por el mismo motivo de concurrencia con ReservaLogica.Crear.
    public async Task<(bool eliminado, string? error)> Eliminar(int id, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var cancha = await _repo.ObtenerPorId(id);
                if (cancha == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                if (cancha.Estado == EstadoCancha.Baja)
                    return (false, "La cancha ya está dada de baja");

                // Se conserva el historial (reservas ya realizadas o canceladas) sin restricción,
                // pero las reservas pendientes futuras hay que resolverlas antes de dar de baja la
                // cancha; de lo contrario quedarían huérfanas.
                var reservas = await _repoReserva.ObtenerTodos();
                var reservasPendientes = reservas.Count(r =>
                    r.Cod_Cancha == id &&
                    r.Estado == EstadoReserva.Pendiente);

                if (reservasPendientes > 0)
                    return (false, $"No se puede dar de baja: tiene {reservasPendientes} reserva(s) pendiente(s) para hoy o fechas futuras. Cancelalas primero.");

                // Baja lógica: se conserva el historial de reservas asociadas.
                var anterior = MapDto(cancha);
                cancha.Estado = EstadoCancha.Baja;
                await _repo.Actualizar(cancha);

                await _auditoria.Registrar(codUsuarioAccion, "Cancha", id, AccionAuditoria.Baja, anterior, null);
                return (true, (string?)null);
            }, IsolationLevel.Serializable);
        }
        catch (OperacionInvalidaException ex)
        {
            return (false, ex.Message);
        }
    }
}
