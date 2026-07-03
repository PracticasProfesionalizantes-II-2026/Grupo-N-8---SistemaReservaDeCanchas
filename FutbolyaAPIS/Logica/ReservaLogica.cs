using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IReservaLogica
{
    Task<IEnumerable<ReservaDto>> ObtenerTodos();
    Task<ReservaDto?> ObtenerPorId(int id);
    Task<(ReservaDto? resultado, string? error)> Crear(ReservaCreateDto dto);
    Task<(ReservaDto? resultado, string? error)> Actualizar(int id, ReservaUpdateDto dto);
    Task Eliminar(int id);
    Task<IEnumerable<ReservaMaterialDto>> ObtenerMateriales(int idReserva);
    Task<(ReservaMaterialDto? resultado, string? error)> AgregarMaterial(int idReserva, ReservaMaterialAddDto dto);
    Task<(bool eliminado, string? error)> QuitarMaterial(int idReserva, int idReservaMat);
}

public class ReservaLogica : IReservaLogica
{
    private readonly IReservaRepository _repo;
    private readonly IReservaMaterialRepository _repoMaterial;
    private readonly IMaterialDeportivoRepository _repoStock;
    private readonly ICanchaRepository _repoCancha;
    private readonly IHorarioDisponibleRepository _repoHorario;
    private readonly IUsuarioRepository _repoUsuario;

    public ReservaLogica(
        IReservaRepository repo,
        IReservaMaterialRepository repoMaterial,
        IMaterialDeportivoRepository repoStock,
        ICanchaRepository repoCancha,
        IHorarioDisponibleRepository repoHorario,
        IUsuarioRepository repoUsuario)
    {
        _repo        = repo;
        _repoMaterial = repoMaterial;
        _repoStock   = repoStock;
        _repoCancha  = repoCancha;
        _repoHorario = repoHorario;
        _repoUsuario = repoUsuario;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static ReservaDto MapDto(Reserva r, bool conMateriales = false) =>
        new ReservaDto(
            r.Cod_Reserva,
            r.Fecha,
            r.FechaReserva,
            r.Dni_Cliente,
            r.Telefono_Cliente,
            r.Cod_Cancha,
            r.Cancha?.Nombre ?? string.Empty,
            r.Cod_Usuario,
            $"{r.Usuario?.Nombre} {r.Usuario?.Apellido}",
            r.Duracion,
            r.Cod_Horario,
            r.HorarioDisponible?.HoraInicio ?? TimeSpan.Zero,
            r.HorarioDisponible?.HoraFin    ?? TimeSpan.Zero,
            conMateriales
                ? r.ReservaMateriales?.Select(rm => new ReservaMaterialDto(
                    rm.Cod_Reserva_Mat,
                    rm.Cod_Reserva,
                    rm.Cod_Material,
                    rm.Material_Deportivo?.Nombre ?? string.Empty,
                    rm.Cantidad))
                : null
        );

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

    public async Task<(ReservaDto? resultado, string? error)> Crear(ReservaCreateDto dto)
    {
        //Verificar que la fecha de reserva no sea en el pasado
        if (dto.FechaReserva.Date < DateTime.Today)
            return (null, "La fecha de reserva no puede ser en el pasado");

        //Verificar que la cancha exista y esté disponible
        var cancha = await _repoCancha.ObtenerPorId(dto.Cod_Cancha);
        if (cancha == null)
            return (null, "Cancha no encontrada");
        if (!cancha.Estado)
            return (null, "La cancha no está disponible actualmente");

        //Verificar que el horario exista y esté activo
        var horario = await _repoHorario.ObtenerPorId(dto.Cod_Horario);
        if (horario == null)
            return (null, "Horario no encontrado");
        if (!horario.Activo)
            return (null, "El horario seleccionado no está activo");

        //Verificar que la cancha no esté reservada en esa fecha y horario
        var reservasExistentes = await _repo.ObtenerTodos();
        var canchaOcupada = reservasExistentes.Any(r =>
            r.Cod_Cancha        == dto.Cod_Cancha  &&
            r.Cod_Horario       == dto.Cod_Horario &&
            r.FechaReserva.Date == dto.FechaReserva.Date);

        if (canchaOcupada)
            return (null, "La cancha ya está reservada para ese horario y fecha");

        //Verificar que el usuario exista
        var usuario = await _repoUsuario.ObtenerPorId(dto.Cod_Usuario);
        if (usuario == null)
            return (null, "Usuario no encontrado");

        // ── Agrupar materiales por id (por si el mismo material aparece más de una vez)
        var cantidadesPorMaterial = dto.Materiales
            .GroupBy(m => m.Cod_Material)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Cantidad));

        //Verificar stock disponible antes de crear
        foreach (var (codMaterial, cantidadTotal) in cantidadesPorMaterial)
        {
            var material = await _repoStock.ObtenerPorId(codMaterial);
            if (material == null)
                return (null, $"Material con id {codMaterial} no encontrado");

            if (material.Cant_Material < cantidadTotal)
                return (null, $"Stock insuficiente para el material '{material.Nombre}'. Disponible: {material.Cant_Material}");
        }

        var reserva = new Reserva
        {
            Fecha            = DateTime.Now, // La fecha de creación se asigna automáticamente
            FechaReserva     = dto.FechaReserva,
            Dni_Cliente      = dto.Dni_Cliente,
            Telefono_Cliente = dto.Telefono_Cliente,
            Cod_Cancha       = dto.Cod_Cancha,
            Cod_Usuario      = dto.Cod_Usuario,
            Duracion         = dto.Duracion,
            Cod_Horario      = dto.Cod_Horario
        };

        await _repo.Agregar(reserva);

        // Descontar stock (agrupado) y crear los materiales de la reserva
        foreach (var (codMaterial, cantidadTotal) in cantidadesPorMaterial)
        {
            var material = await _repoStock.ObtenerPorId(codMaterial);
            material!.Cant_Material -= cantidadTotal;
            await _repoStock.Actualizar(material);

            var rm = new Reserva_Material
            {
                Cod_Reserva  = reserva.Cod_Reserva,
                Cod_Material = codMaterial,
                Cantidad     = cantidadTotal
            };
            await _repoMaterial.Agregar(rm);
        }

        var creada = await _repo.ObtenerPorId(reserva.Cod_Reserva);
        return (MapDto(creada!, conMateriales: true), null);
    }

    public async Task<(ReservaDto? resultado, string? error)> Actualizar(int id, ReservaUpdateDto dto)
    {
        var reserva = await _repo.ObtenerPorId(id);
        if (reserva == null)
            return (null, "Reserva no encontrada");

        //Verificar que la fecha de reserva no sea en el pasado
        if (dto.FechaReserva.Date < DateTime.Today)
            return (null, "La fecha de reserva no puede ser en el pasado");

        //Verificar que la cancha exista y esté disponible
        var cancha = await _repoCancha.ObtenerPorId(dto.Cod_Cancha);
        if (cancha == null)
            return (null, "Cancha no encontrada");
        if (!cancha.Estado)
            return (null, "La cancha no está disponible actualmente");

        //Verificar que el horario exista y esté activo
        var horario = await _repoHorario.ObtenerPorId(dto.Cod_Horario);
        if (horario == null)
            return (null, "Horario no encontrado");
        if (!horario.Activo)
            return (null, "El horario seleccionado no está activo");

        //Verificar que la cancha no esté ocupada en esa fecha y horario
        var reservasExistentes = await _repo.ObtenerTodos();
        var canchaOcupada = reservasExistentes.Any(r =>
            r.Cod_Reserva       != id              &&
            r.Cod_Cancha        == dto.Cod_Cancha  &&
            r.Cod_Horario       == dto.Cod_Horario &&
            r.FechaReserva.Date == dto.FechaReserva.Date);

        if (canchaOcupada)
            return (null, "La cancha ya está reservada para ese horario y fecha");

        // ── Validar stock de los NUEVOS materiales ANTES de tocar nada ──
        // Agrupamos por si el mismo material aparece más de una vez en el detalle
        var cantidadesPorMaterial = dto.Materiales
            .GroupBy(m => m.Cod_Material)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Cantidad));

        // Stock que quedaría liberado de los materiales viejos, por material
        var stockLiberadoPorMaterial = (reserva.ReservaMateriales ?? Enumerable.Empty<Reserva_Material>())
            .GroupBy(rm => rm.Cod_Material)
            .ToDictionary(g => g.Key, g => g.Sum(rm => rm.Cantidad));

        foreach (var (codMaterial, cantidadNueva) in cantidadesPorMaterial)
        {
            var material = await _repoStock.ObtenerPorId(codMaterial);
            if (material == null)
                return (null, $"Material con id {codMaterial} no encontrado");

            // Stock disponible real = stock actual + lo que se liberaría de ESTA reserva para ese material
            stockLiberadoPorMaterial.TryGetValue(codMaterial, out var liberado);
            var stockDisponible = material.Cant_Material + liberado;

            if (stockDisponible < cantidadNueva)
                return (null, $"Stock insuficiente para el material '{material.Nombre}'. Disponible: {stockDisponible}");
        }

        // ── Recién ahora, con TODO validado, aplicamos los cambios ──

        // Liberar stock de materiales anteriores
        foreach (var rm in reserva.ReservaMateriales ?? Enumerable.Empty<Reserva_Material>())
        {
            var material = await _repoStock.ObtenerPorId(rm.Cod_Material);
            if (material != null)
            {
                material.Cant_Material += rm.Cantidad;
                await _repoStock.Actualizar(material);
            }
            await _repoMaterial.Eliminar(rm);
        }

        // Actualizar datos de la reserva
        reserva.FechaReserva     = dto.FechaReserva;
        reserva.Dni_Cliente      = dto.Dni_Cliente;
        reserva.Telefono_Cliente = dto.Telefono_Cliente;
        reserva.Cod_Cancha       = dto.Cod_Cancha;
        reserva.Cod_Horario      = dto.Cod_Horario;
        reserva.Duracion         = dto.Duracion;

        await _repo.Actualizar(reserva);

        // Descontar stock (agrupado) y crear nuevos materiales
        foreach (var (codMaterial, cantidadTotal) in cantidadesPorMaterial)
        {
            var material = await _repoStock.ObtenerPorId(codMaterial);
            material!.Cant_Material -= cantidadTotal;
            await _repoStock.Actualizar(material);

            var rm = new Reserva_Material
            {
                Cod_Reserva  = reserva.Cod_Reserva,
                Cod_Material = codMaterial,
                Cantidad     = cantidadTotal
            };
            await _repoMaterial.Agregar(rm);
        }

        var actualizada = await _repo.ObtenerPorId(id);
        return (MapDto(actualizada!, conMateriales: true), null);
    }

    public async Task Eliminar(int id)
    {
        var reserva = await _repo.ObtenerPorId(id);
        if (reserva == null) return;

        // Liberar stock al cancelar
        foreach (var rm in reserva.ReservaMateriales ?? Enumerable.Empty<Reserva_Material>())
        {
            var material = await _repoStock.ObtenerPorId(rm.Cod_Material);
            if (material != null)
            {
                material.Cant_Material += rm.Cantidad;
                await _repoStock.Actualizar(material);
            }
        }

        await _repo.Eliminar(reserva);
    }

    public async Task<IEnumerable<ReservaMaterialDto>> ObtenerMateriales(int idReserva)
    {
        var reserva = await _repo.ObtenerPorId(idReserva);
        if (reserva == null) return Enumerable.Empty<ReservaMaterialDto>();

        return reserva.ReservaMateriales?.Select(rm => new ReservaMaterialDto(
            rm.Cod_Reserva_Mat,
            rm.Cod_Reserva,
            rm.Cod_Material,
            rm.Material_Deportivo?.Nombre ?? string.Empty,
            rm.Cantidad
        )) ?? Enumerable.Empty<ReservaMaterialDto>();
    }

    public async Task<(ReservaMaterialDto? resultado, string? error)> AgregarMaterial(int idReserva, ReservaMaterialAddDto dto)
    {
        var reserva = await _repo.ObtenerPorId(idReserva);
        if (reserva == null)
            return (null, "Reserva no encontrada");

        var material = await _repoStock.ObtenerPorId(dto.Cod_Material);
        if (material == null)
            return (null, "Material no encontrado");

        if (material.Cant_Material < dto.Cantidad)
            return (null, $"Stock insuficiente para '{material.Nombre}'. Disponible: {material.Cant_Material}");

        material.Cant_Material -= dto.Cantidad;
        await _repoStock.Actualizar(material);

        var rmExistente = reserva.ReservaMateriales?.FirstOrDefault(r => r.Cod_Material == dto.Cod_Material);
        if (rmExistente != null)
        {
            rmExistente.Cantidad += dto.Cantidad;
            await _repoMaterial.Actualizar(rmExistente);

            return (new ReservaMaterialDto(
                rmExistente.Cod_Reserva_Mat,
                rmExistente.Cod_Reserva,
                rmExistente.Cod_Material,
                material.Nombre,
                rmExistente.Cantidad
            ), null);
        }
        else
        {
            var rm = new Reserva_Material
            {
                Cod_Reserva  = idReserva,       
                Cod_Material = dto.Cod_Material,
                Cantidad     = dto.Cantidad
            };
            await _repoMaterial.Agregar(rm);

            return (new ReservaMaterialDto(
                rm.Cod_Reserva_Mat,
                rm.Cod_Reserva,
                rm.Cod_Material,
                material.Nombre,
                rm.Cantidad
            ), null);
        }
    }

    public async Task<(bool eliminado, string? error)> QuitarMaterial(int idReserva, int idMaterial)
    {
        var reserva = await _repo.ObtenerPorId(idReserva);
        if (reserva == null)
            return (false, "Reserva no encontrada");

        var material = await _repoStock.ObtenerPorId(idMaterial);
        if (material == null)
            return (false, "Material no encontrado");
        
        var rm = reserva.ReservaMateriales?.FirstOrDefault(r => r.Cod_Material == idMaterial);
        if (rm == null)
            return (false, "Este material no está asociado a esta reserva");

        material.Cant_Material += rm.Cantidad;
        await _repoStock.Actualizar(material);
        await _repoMaterial.Eliminar(rm);
        return (true, null);
    }
}