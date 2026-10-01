using System.Data;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Logica.Excepciones;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IHorarioDisponibleLogica
{
    Task<IEnumerable<HorarioDisponibleDto>> ObtenerTodos();
    Task<IEnumerable<HorarioDisponibleDto>> ObtenerPorCancha(int codCancha);
    Task<HorarioDisponibleDto?> ObtenerPorId(int id);
    Task<(HorarioDisponibleDto? resultado, string? error)> ActualizarActivo(int id, bool activo, int codUsuarioAccion);
}

// Expone el catálogo global de horarios disponibles (24 bloques fijos de 1 hora, sembrados por
// migración) entre los Endpoints y los Repositorios. No existe alta ni edición individual de
// horarios: la única operación de escritura es activar/desactivar un bloque del catálogo.
public class HorarioDisponibleLogica : IHorarioDisponibleLogica
{
    private readonly IHorarioDisponibleRepository _repo;
    private readonly IReservaRepository _repoReserva;
    private readonly IAuditoriaLogica _auditoria;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public HorarioDisponibleLogica(
        IHorarioDisponibleRepository repo,
        IReservaRepository repoReserva,
        IAuditoriaLogica auditoria,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _repo            = repo;
        _repoReserva     = repoReserva;
        _auditoria       = auditoria;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    private static HorarioDisponibleDto MapDto(HorarioDisponible h) =>
        new(h.Cod_Horario, h.HoraInicio, h.HoraFin, h.Activo);

    public async Task<IEnumerable<HorarioDisponibleDto>> ObtenerTodos()
    {
        var horarios = await _repo.ObtenerTodos();
        return horarios.Select(MapDto);
    }

    public async Task<IEnumerable<HorarioDisponibleDto>> ObtenerPorCancha(int codCancha)
    {
        var horarios = await _repo.ObtenerPorCancha(codCancha);
        return horarios.Select(MapDto);
    }

    public async Task<HorarioDisponibleDto?> ObtenerPorId(int id)
    {
        var h = await _repo.ObtenerPorId(id);
        return h == null ? null : MapDto(h);
    }

    // Activa o desactiva (baja lógica) un bloque horario del catálogo; es mantenimiento
    // administrativo, sin UI, vía Scalar. La verificación de reservas pendientes y el toggle de
    // Activo corren en una transacción Serializable, por el mismo motivo de concurrencia que
    // CanchaLogica.ActualizarEstado.
    public async Task<(HorarioDisponibleDto? resultado, string? error)> ActualizarActivo(int id, bool activo, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var horario = await _repo.ObtenerPorId(id);
                if (horario == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                // Si se intenta desactivar (baja lógica), verificar que no tenga reservas Pendientes
                // (fecha/hora todavía no ocurrida) asociadas. Una reserva ya Confirmada (pasada) es
                // historial y no debería bloquear la baja.
                if (!activo)
                {
                    var reservas = await _repoReserva.ObtenerTodos();
                    var tieneReservasActivas = reservas.Any(r =>
                        r.Estado == EstadoReserva.Pendiente &&
                        r.ReservaHorarios.Any(rh => rh.Cod_Horario == id));

                    if (tieneReservasActivas)
                        throw new OperacionInvalidaException("No se puede dar de baja un horario con reservas pendientes asociadas");
                }

                var anterior = MapDto(horario);
                horario.Activo = activo;
                await _repo.Actualizar(horario);

                var resultado = MapDto(horario);
                await _auditoria.Registrar(codUsuarioAccion, "HorarioDisponible", id, activo ? AccionAuditoria.Modificacion : AccionAuditoria.Baja, anterior, resultado);
                return (resultado, (string?)null);
            }, IsolationLevel.Serializable);
        }
        catch (OperacionInvalidaException ex)
        {
            return (null, ex.Message);
        }
    }
}
