using System.Data;
using System.Text.RegularExpressions;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Logica.Excepciones;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IReservaLogica
{
    Task<IEnumerable<ReservaDto>> ObtenerTodos();
    Task<ReservaDto?> ObtenerPorId(int id);
    Task<(ReservaDto? resultado, string? error)> Crear(ReservaCreateDto dto, int codUsuarioAccion);
    Task<(bool eliminado, string? error)> Cancelar(int id, int codUsuarioAccion);

    // Pública porque la llama periódicamente BarridoReservasService (Servicios/); los GET
    // quedan de solo lectura.
    Task ConfirmarVencidas();
}

// Valida y ejecuta las operaciones de negocio sobre reservas (alta, cancelación, barrido de
// vencidas): chequeo de datos del cliente, disponibilidad de horarios y stock, y registro de
// auditoría. Se ubica entre los Endpoints (delgados) y los Repositorios.
public class ReservaLogica : IReservaLogica
{
    // Validación de formato server-side, independiente de la validación en el cliente.
    private static readonly Regex RegexDni      = new(@"^\d{7,8}$", RegexOptions.Compiled);
    private static readonly Regex RegexTelefono = new(@"^\d{6,15}$", RegexOptions.Compiled);

    private readonly IReservaRepository _repo;
    private readonly IReservaMaterialRepository _repoMaterial;
    private readonly IMaterialDeportivoRepository _repoStock;
    private readonly ICanchaRepository _repoCancha;
    private readonly IHorarioDisponibleRepository _repoHorario;
    private readonly IUsuarioRepository _repoUsuario;
    private readonly IReservaHorarioRepository _repoReservaHorario;
    private readonly IAuditoriaLogica _auditoria;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger<ReservaLogica> _logger;

    public ReservaLogica(
        IReservaRepository repo,
        IReservaMaterialRepository repoMaterial,
        IMaterialDeportivoRepository repoStock,
        ICanchaRepository repoCancha,
        IHorarioDisponibleRepository repoHorario,
        IUsuarioRepository repoUsuario,
        IReservaHorarioRepository repoReservaHorario,
        IAuditoriaLogica auditoria,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger<ReservaLogica> logger)
    {
        _repo               = repo;
        _repoMaterial       = repoMaterial;
        _repoStock          = repoStock;
        _repoCancha         = repoCancha;
        _repoHorario        = repoHorario;
        _repoUsuario        = repoUsuario;
        _repoReservaHorario = repoReservaHorario;
        _auditoria          = auditoria;
        _unidadDeTrabajo    = unidadDeTrabajo;
        _logger             = logger;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static ReservaDto MapDto(Reserva r, bool conMateriales = false)
    {
        var horarios = (r.ReservaHorarios ?? Enumerable.Empty<Reserva_Horario>())
            .OrderBy(rh => rh.HorarioDisponible.HoraInicio)
            .Select(rh => new HorarioBloqueDto(rh.Cod_Horario, rh.HorarioDisponible.HoraInicio, rh.HorarioDisponible.HoraFin))
            .ToList();

        return new ReservaDto(
            r.Cod_Reserva,
            r.Fecha,
            r.FechaReserva,
            r.Dni_Cliente,
            r.Telefono_Cliente,
            r.Cod_Cancha,
            r.Cancha?.Nombre ?? string.Empty,
            r.Cod_Usuario,
            $"{r.Usuario?.Nombre} {r.Usuario?.Apellido}",
            r.Estado.ToString(),
            horarios.Count,
            horarios,
            conMateriales
                ? r.ReservaMateriales?.Select(rm => new ReservaMaterialDto(
                    rm.Cod_Reserva_Mat,
                    rm.Cod_Reserva,
                    rm.Cod_Material,
                    rm.Material_Deportivo?.Nombre ?? string.Empty,
                    rm.Cantidad))
                : null
        );
    }

    /// <summary>
    /// Valida que los horarios elegidos existan, pertenezcan a la cancha, estén activos
    /// y formen un bloque continuo (sin huecos entre ellos). Devuelve la lista ordenada
    /// por HoraInicio, o un mensaje de error.
    /// </summary>
    private async Task<(List<HorarioDisponible>? horarios, string? error)> ValidarBloqueContinuo(int codCancha, IEnumerable<int> codHorarios)
    {
        var ids = codHorarios.Distinct().ToList();
        if (ids.Count == 0)
            return (null, "Debe seleccionar al menos un horario");

        // Los horarios son un catálogo global; lo que varía por cancha es qué bloques ofrece.
        var habilitadosPorCancha = (await _repoHorario.ObtenerPorCancha(codCancha)).ToDictionary(h => h.Cod_Horario);

        var horarios = new List<HorarioDisponible>();
        foreach (var id in ids)
        {
            if (!habilitadosPorCancha.TryGetValue(id, out var horario))
                return (null, $"El horario {id} no está habilitado para la cancha seleccionada");
            if (!horario.Activo)
                return (null, "Uno de los horarios seleccionados no está activo");

            horarios.Add(horario);
        }

        horarios = horarios.OrderBy(h => h.HoraInicio).ToList();

        for (int i = 0; i < horarios.Count - 1; i++)
        {
            if (horarios[i].HoraFin != horarios[i + 1].HoraInicio)
                return (null, "El rango seleccionado no es continuo. Seleccioná horas consecutivas.");
        }

        return (horarios, null);
    }

    private static string? ValidarDatosCliente(string dni, string telefono)
    {
        if (!RegexDni.IsMatch(dni))
            return "El DNI del cliente debe tener entre 7 y 8 dígitos numéricos";
        if (!RegexTelefono.IsMatch(telefono))
            return "El teléfono del cliente debe tener entre 6 y 15 dígitos numéricos";
        return null;
    }

    // Rechaza bloques cuya hora de inicio ya pasó, cuando la reserva es para hoy.
    private static string? ValidarBloquesNoIniciados(DateTime fechaReserva, IEnumerable<HorarioDisponible> horarios)
    {
        if (fechaReserva.Date != DateTime.Today)
            return null;

        if (horarios.Any(h => h.HoraInicio <= DateTime.Now.TimeOfDay))
            return "No se puede reservar un horario que ya comenzó";

        return null;
    }

    // ¿Ya pasó la fecha y hora de esta reserva? Se compara contra el fin del último bloque
    // horario (no solo la fecha), para no marcarla como "realizada" mientras todavía está en curso.
    private static bool YaPaso(Reserva r)
    {
        if (r.FechaReserva.Date < DateTime.Today) return true;
        if (r.FechaReserva.Date > DateTime.Today) return false;

        var finUltimoBloque = r.ReservaHorarios
            .Select(rh => rh.HorarioDisponible.HoraFin)
            .DefaultIfEmpty(TimeSpan.Zero)
            .Max();

        return finUltimoBloque <= DateTime.Now.TimeOfDay;
    }

    // Recorre las reservas Pendientes cuya fecha/hora ya pasó, las marca Confirmada (se asume
    // que se usaron) y devuelve el material reservado al stock. La llama periódicamente
    // BarridoReservasService (config "Reservas:MinutosBarrido", default 5) y una vez al
    // arrancar; los GET son de solo lectura, así que el Estado que devuelve GET /api/reservas
    // puede tardar hasta N minutos en reflejar que una reserva venció. No se calcula el estado
    // on-the-fly en el mapeo porque eso duplicaría la regla de negocio entre el barrido y el DTO.
    //
    // Cada reserva vencida se procesa en su propia transacción (no una transacción para todo el
    // lote): así una reserva con un problema no bloquea que se confirmen las demás, y un barrido
    // interrumpido a la mitad no deja transacciones largas abiertas.
    //
    // Atribución de auditoría: el barrido no tiene un usuario humano detrás, así que el registro
    // se atribuye al usuario que hizo la reserva original (reserva.Cod_Usuario) en vez de una fila
    // de Usuario "sistema": Auditoria.Cod_Usuario es FK obligatoria y no soft-deleteable, así que
    // una fila ficticia quedaría para siempre en el catálogo de usuarios solo para este caso.
    public async Task ConfirmarVencidas()
    {
        var reservas = await _repo.ObtenerTodos();
        var vencidas = reservas.Where(r => r.Estado == EstadoReserva.Pendiente && YaPaso(r)).ToList();

        foreach (var reserva in vencidas)
        {
            try
            {
                await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
                {
                    foreach (var rm in reserva.ReservaMateriales ?? Enumerable.Empty<Reserva_Material>())
                    {
                        await _repoStock.RestaurarStock(rm.Cod_Material, rm.Cantidad);
                    }

                    reserva.Estado = EstadoReserva.Confirmada;
                    await _repo.Actualizar(reserva);

                    await _auditoria.Registrar(reserva.Cod_Usuario, "Reserva", reserva.Cod_Reserva, AccionAuditoria.Modificacion,
                        new { Estado = "Pendiente" }, new { Estado = "Confirmada", Motivo = "Venció la fecha/hora de la reserva" });

                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar la reserva vencida {CodReserva} en el barrido", reserva.Cod_Reserva);
            }
        }
    }

    public async Task<IEnumerable<ReservaDto>> ObtenerTodos()
    {
        var reservas = await _repo.ObtenerTodos();
        return reservas.Select(r => MapDto(r));
    }

    public async Task<ReservaDto?> ObtenerPorId(int id)
    {
        var r = await _repo.ObtenerPorId(id);
        if (r == null) return null;
        return MapDto(r, conMateriales: true);
    }

    // Crear hace varias escrituras (Reserva + Reserva_Horario por bloque +
    // Reserva_Material + descuento de stock) y además necesita evitar el doble booking: dos
    // requests concurrentes podían leer "libre" para el mismo horario antes de que ninguna
    // hubiera insertado todavía (TOCTOU clásico), reservando el mismo bloque dos veces.
    //
    // Se eligió aislamiento Serializable para toda la transacción (en vez de sp_getapplock o un
    // lock hint con SQL crudo): con Serializable, SQL Server toma range locks sobre las filas que
    // lee la verificación de solapamiento (Reserva/Reserva_Horario para esa cancha+fecha), así que
    // una segunda transacción concurrente que intente insertar una reserva conflictiva para el
    // mismo rango queda bloqueada hasta que la primera termine, y si igual se detecta un conflicto
    // de serialización, SQL Server la aborta con el error 1205/3960 que UnidadDeTrabajo reintenta
    // automáticamente. Es la opción más simple que no requiere SQL crudo en la Logica (que pedía
    // el diseño) y el volumen de reservas concurrentes por cancha es bajo, así que el costo extra
    // de bloqueo de Serializable no es un problema de performance acá.
    public async Task<(ReservaDto? resultado, string? error)> Crear(ReservaCreateDto dto, int codUsuarioAccion)
    {
        if (dto.FechaReserva.Date < DateTime.Today)
            return (null, "La fecha de reserva no puede ser en el pasado");

        var errorCliente = ValidarDatosCliente(dto.Dni_Cliente, dto.Telefono_Cliente);
        if (errorCliente != null)
            return (null, errorCliente);

        if (dto.Materiales.Any(m => m.Cantidad <= 0))
            return (null, "La cantidad de cada material debe ser mayor a 0");

        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var cancha = await _repoCancha.ObtenerPorId(dto.Cod_Cancha);
                if (cancha == null)
                    throw new OperacionInvalidaException("Cancha no encontrada");
                if (cancha.Estado != EstadoCancha.Disponible)
                    throw new OperacionInvalidaException("La cancha no está disponible actualmente");

                var (horarios, errorHorario) = await ValidarBloqueContinuo(dto.Cod_Cancha, dto.Cod_Horarios);
                if (errorHorario != null)
                    throw new OperacionInvalidaException(errorHorario);

                var errorNoIniciado = ValidarBloquesNoIniciados(dto.FechaReserva, horarios!);
                if (errorNoIniciado != null)
                    throw new OperacionInvalidaException(errorNoIniciado);

                // Verificar que ninguno de los bloques esté ya ocupado por otra reserva no cancelada
                // (Pendiente o ya Confirmada; esta última ya pasó de fecha, así que en la práctica
                // nunca va a chocar con una fecha futura, pero no cuesta nada ser explícitos).
                // Bajo Serializable, esta lectura deja el rango bloqueado hasta el commit/rollback.
                var reservasExistentes = await _repo.ObtenerTodos();
                var idsHorario = horarios!.Select(h => h.Cod_Horario).ToHashSet();
                var ocupado = reservasExistentes.Any(r =>
                    r.Estado != EstadoReserva.Cancelada &&
                    r.Cod_Cancha == dto.Cod_Cancha &&
                    r.FechaReserva.Date == dto.FechaReserva.Date &&
                    r.ReservaHorarios.Any(rh => idsHorario.Contains(rh.Cod_Horario)));

                if (ocupado)
                    throw new OperacionInvalidaException("Rango inválido: uno o más horarios ya están reservados para esa fecha");

                // Cod_Usuario se toma del JWT autenticado, no del cuerpo enviado por el cliente
                // (que podría falsificarse).
                var usuario = await _repoUsuario.ObtenerPorId(codUsuarioAccion);
                if (usuario == null)
                    throw new OperacionInvalidaException("Usuario no encontrado");

                var cantidadesPorMaterial = dto.Materiales
                    .GroupBy(m => m.Cod_Material)
                    .ToDictionary(g => g.Key, g => g.Sum(m => m.Cantidad));

                var materiales = new Dictionary<int, Material_Deportivo>();
                foreach (var (codMaterial, cantidadTotal) in cantidadesPorMaterial)
                {
                    var material = await _repoStock.ObtenerPorId(codMaterial);
                    if (material == null)
                        throw new OperacionInvalidaException($"Material con id {codMaterial} no encontrado");
                    // También valida que el material esté activo: uno dado de baja no debe poder
                    // reservarse aunque tenga stock.
                    if (!material.Activo)
                        throw new OperacionInvalidaException($"El material '{material.Nombre}' no está disponible");
                    if (material.Cant_Material < cantidadTotal)
                        throw new OperacionInvalidaException($"Stock insuficiente para el material '{material.Nombre}'. Disponible: {material.Cant_Material}");

                    materiales[codMaterial] = material;
                }

                var reserva = new Reserva
                {
                    Fecha            = DateTime.Now,
                    FechaReserva     = dto.FechaReserva,
                    Dni_Cliente      = dto.Dni_Cliente,
                    Telefono_Cliente = dto.Telefono_Cliente,
                    Cod_Cancha       = dto.Cod_Cancha,
                    Cod_Usuario      = codUsuarioAccion,
                    Estado           = EstadoReserva.Pendiente
                };

                await _repo.Agregar(reserva);

                foreach (var horario in horarios!)
                {
                    await AgregarBloqueHorario(reserva.Cod_Reserva, horario.Cod_Horario);
                }

                foreach (var (codMaterial, cantidadTotal) in cantidadesPorMaterial)
                {
                    // Update atómico condicional; ver el comentario en ProductoRepository.
                    // No volver a llamar _repoStock.Actualizar(...) sobre el Material_Deportivo
                    // ya cargado en `materiales` para esta reserva.
                    var descontado = await _repoStock.DescontarStock(codMaterial, cantidadTotal);
                    if (!descontado)
                        throw new OperacionInvalidaException($"Stock insuficiente para el material '{materiales[codMaterial].Nombre}'. Disponible: {materiales[codMaterial].Cant_Material}");

                    await _repoMaterial.Agregar(new Reserva_Material
                    {
                        Cod_Reserva  = reserva.Cod_Reserva,
                        Cod_Material = codMaterial,
                        Cantidad     = cantidadTotal
                    });
                }

                var creada = await _repo.ObtenerPorId(reserva.Cod_Reserva);
                var dtoCreada = MapDto(creada!, conMateriales: true);
                await _auditoria.Registrar(codUsuarioAccion, "Reserva", reserva.Cod_Reserva, AccionAuditoria.Alta, null, dtoCreada);
                return (dtoCreada, (string?)null);
            }, IsolationLevel.Serializable);
        }
        catch (OperacionInvalidaException ex)
        {
            return (null, ex.Message);
        }
    }

    private Task AgregarBloqueHorario(int codReserva, int codHorario) =>
        _repoReservaHorario.Agregar(new Reserva_Horario { Cod_Reserva = codReserva, Cod_Horario = codHorario });

    public async Task<(bool eliminado, string? error)> Cancelar(int id, int codUsuarioAccion)
    {
        try
        {
            return await _unidadDeTrabajo.EjecutarEnTransaccion(async () =>
            {
                var reserva = await _repo.ObtenerPorId(id);
                if (reserva == null)
                    throw new OperacionInvalidaException("NOT_FOUND");

                if (reserva.Estado == EstadoReserva.Cancelada)
                    return (false, "La reserva ya estaba cancelada");
                if (reserva.Estado == EstadoReserva.Confirmada)
                    return (false, "No se puede cancelar una reserva que ya se realizó");

                var anterior = MapDto(reserva, conMateriales: true);

                // Liberar stock al cancelar (incremento atómico); se conservan los bloques
                // horarios como historial.
                foreach (var rm in reserva.ReservaMateriales ?? Enumerable.Empty<Reserva_Material>())
                {
                    await _repoStock.RestaurarStock(rm.Cod_Material, rm.Cantidad);
                }

                reserva.Estado = EstadoReserva.Cancelada;
                await _repo.Actualizar(reserva);

                var nuevo = MapDto(reserva, conMateriales: true);
                await _auditoria.Registrar(codUsuarioAccion, "Reserva", id, AccionAuditoria.Baja, anterior, nuevo);
                return (true, (string?)null);
            });
        }
        catch (OperacionInvalidaException ex)
        {
            return (false, ex.Message);
        }
    }
}
