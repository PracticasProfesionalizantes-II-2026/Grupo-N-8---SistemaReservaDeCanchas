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

    public ReservaLogica(
        IReservaRepository repo,
        IReservaMaterialRepository repoMaterial,
        IMaterialDeportivoRepository repoStock)
    {
        _repo         = repo;
        _repoMaterial = repoMaterial;
        _repoStock    = repoStock;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static ReservaDto MapDto(Reserva r, bool conMateriales = false) =>
        new ReservaDto(
            r.Cod_Reserva,
            r.Fecha,
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

    // ── ObtenerTodos ───────────────────────────────────────────────────
    public async Task<IEnumerable<ReservaDto>> ObtenerTodos()
    {
        var reservas = await _repo.ObtenerTodos();
        return reservas.Select(r => MapDto(r));
    }

    // ── ObtenerPorId ───────────────────────────────────────────────────
    public async Task<ReservaDto?> ObtenerPorId(int id)
    {
        var r = await _repo.ObtenerPorId(id);
        if (r == null) return null;
        return MapDto(r, conMateriales: true);
    }

    // ── Crear ──────────────────────────────────────────────────────────
    public async Task<(ReservaDto? resultado, string? error)> Crear(ReservaCreateDto dto)
    {
        // Verificar stock disponible antes de crear
        foreach (var item in dto.Materiales)
        {
            var material = await _repoStock.ObtenerPorId(item.Cod_Material);
            if (material == null)
                return (null, "NOT_FOUND");

            if (material.Cant_Material < item.Cantidad)
                return (null, $"Stock insuficiente para el material '{material.Nombre}'. Disponible: {material.Cant_Material}");
        }

        var reserva = new Reserva
        {
            Fecha            = dto.Fecha,
            Dni_Cliente      = dto.Dni_Cliente,
            Telefono_Cliente = dto.Telefono_Cliente,
            Cod_Cancha       = dto.Cod_Cancha,
            Cod_Usuario      = dto.Cod_Usuario,
            Duracion         = dto.Duracion,
            Cod_Horario      = dto.Cod_Horario
        };

        await _repo.Agregar(reserva);

        // Descontar stock y crear los materiales de la reserva
        foreach (var item in dto.Materiales)
        {
            var material = await _repoStock.ObtenerPorId(item.Cod_Material);
            material!.Cant_Material -= item.Cantidad;
            await _repoStock.Actualizar(material);

            var rm = new Reserva_Material
            {
                Cod_Reserva  = reserva.Cod_Reserva,
                Cod_Material = item.Cod_Material,
                Cantidad     = item.Cantidad
            };
            await _repoMaterial.Agregar(rm);
        }

        var creada = await _repo.ObtenerPorId(reserva.Cod_Reserva);
        return (MapDto(creada!, conMateriales: true), null);
    }

    // ── Actualizar ─────────────────────────────────────────────────────
    public async Task<(ReservaDto? resultado, string? error)> Actualizar(int id, ReservaUpdateDto dto)
    {
        var reserva = await _repo.ObtenerPorId(id);
        if (reserva == null)
            return (null, "NOT_FOUND");

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

        // Verificar stock de nuevos materiales
        foreach (var item in dto.Materiales)
        {
            var material = await _repoStock.ObtenerPorId(item.Cod_Material);
            if (material == null)
                return (null, "NOT_FOUND");

            if (material.Cant_Material < item.Cantidad)
                return (null, $"Stock insuficiente para el material '{material.Nombre}'. Disponible: {material.Cant_Material}");
        }

        // Actualizar datos de la reserva
        reserva.Fecha            = dto.Fecha;
        reserva.Dni_Cliente      = dto.Dni_Cliente;
        reserva.Telefono_Cliente = dto.Telefono_Cliente;
        reserva.Cod_Cancha       = dto.Cod_Cancha;
        reserva.Cod_Horario      = dto.Cod_Horario;
        reserva.Duracion         = dto.Duracion;

        await _repo.Actualizar(reserva);

        // Descontar stock y crear nuevos materiales
        foreach (var item in dto.Materiales)
        {
            var material = await _repoStock.ObtenerPorId(item.Cod_Material);
            material!.Cant_Material -= item.Cantidad;
            await _repoStock.Actualizar(material);

            var rm = new Reserva_Material
            {
                Cod_Reserva  = reserva.Cod_Reserva,
                Cod_Material = item.Cod_Material,
                Cantidad     = item.Cantidad
            };
            await _repoMaterial.Agregar(rm);
        }

        var actualizada = await _repo.ObtenerPorId(id);
        return (MapDto(actualizada!, conMateriales: true), null);
    }

    // ── Eliminar ───────────────────────────────────────────────────────
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

    // ── ObtenerMateriales ──────────────────────────────────────────────
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

    // ── AgregarMaterial ────────────────────────────────────────────────
    public async Task<(ReservaMaterialDto? resultado, string? error)> AgregarMaterial(int idReserva, ReservaMaterialAddDto dto)
    {
        var reserva = await _repo.ObtenerPorId(idReserva);
        if (reserva == null)
            return (null, "NOT_FOUND");

        var material = await _repoStock.ObtenerPorId(dto.Cod_Material);
        if (material == null)
            return (null, "NOT_FOUND");

        if (material.Cant_Material < dto.Cantidad)
            return (null, $"Stock insuficiente para '{material.Nombre}'. Disponible: {material.Cant_Material}");

        material.Cant_Material -= dto.Cantidad;
        await _repoStock.Actualizar(material);

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

    // ── QuitarMaterial ─────────────────────────────────────────────────
    public async Task<(bool eliminado, string? error)> QuitarMaterial(int idReserva, int idReservaMat)
    {
        var reserva = await _repo.ObtenerPorId(idReserva);
        if (reserva == null)
            return (false, "NOT_FOUND");

        var rm = await _repoMaterial.ObtenerPorId(idReservaMat);
        if (rm == null)
            return (false, "NOT_FOUND");

        // Liberar stock al quitar el material
        var material = await _repoStock.ObtenerPorId(rm.Cod_Material);
        if (material != null)
        {
            material.Cant_Material += rm.Cantidad;
            await _repoStock.Actualizar(material);
        }

        await _repoMaterial.Eliminar(rm);
        return (true, null);
    }
}