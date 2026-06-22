using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IMaterialDeportivoLogica
{
    Task<IEnumerable<MaterialDeportivoDto>> ObtenerTodos();
    Task<MaterialDeportivoDto?> ObtenerPorId(int id);
    Task<(MaterialDeportivoDto? resultado, string? error)> Crear(MaterialDeportivoCreateDto dto);
    Task<(MaterialDeportivoDto? resultado, string? error)> Actualizar(int id, MaterialDeportivoCreateDto dto);
    Task<(MaterialDeportivoDto? resultado, string? error)> ActualizarStock(int id, int cantMaterial);
    Task<(bool eliminado, string? error)> Eliminar(int id);
}

public class MaterialDeportivoLogica : IMaterialDeportivoLogica
{
    private readonly IMaterialDeportivoRepository _repo;
    private readonly IReservaMaterialRepository _repoReservaMaterial;

    public MaterialDeportivoLogica(
        IMaterialDeportivoRepository repo,
        IReservaMaterialRepository repoReservaMaterial)
    {
        _repo                = repo;
        _repoReservaMaterial = repoReservaMaterial;
    }

    // ── Mapeo privado ──────────────────────────────────────────────────
    private static MaterialDeportivoDto MapDto(Material_Deportivo m) =>
        new MaterialDeportivoDto(
            m.Cod_Material,
            m.Nombre,
            m.Cant_Material
        );

    public async Task<IEnumerable<MaterialDeportivoDto>> ObtenerTodos()
    {
        var materiales = await _repo.ObtenerTodos();
        return materiales.Select(MapDto);
    }

    public async Task<MaterialDeportivoDto?> ObtenerPorId(int id)
    {
        var m = await _repo.ObtenerPorId(id);
        if (m == null) return null;
        return MapDto(m);
    }

    public async Task<(MaterialDeportivoDto? resultado, string? error)> Crear(MaterialDeportivoCreateDto dto)
    {
        var materiales = await _repo.ObtenerTodos();

        if (materiales.Any(m => m.Nombre.Equals(dto.Nombre, StringComparison.OrdinalIgnoreCase)))
            return (null, "Ya existe un material con ese nombre");

        var material = new Material_Deportivo
        {
            Nombre        = dto.Nombre,
            Cant_Material = dto.Cant_Material
        };

        await _repo.Agregar(material);
        return (MapDto(material), null);
    }

    public async Task<(MaterialDeportivoDto? resultado, string? error)> Actualizar(int id, MaterialDeportivoCreateDto dto)
    {
        var material = await _repo.ObtenerPorId(id);
        if (material == null)
            return (null, "NOT_FOUND");

        material.Nombre        = dto.Nombre;
        material.Cant_Material = dto.Cant_Material;

        await _repo.Actualizar(material);
        return (MapDto(material), null);
    }

    public async Task<(MaterialDeportivoDto? resultado, string? error)> ActualizarStock(int id, int cantMaterial)
    {
        var material = await _repo.ObtenerPorId(id);
        if (material == null)
            return (null, "NOT_FOUND");

        material.Cant_Material = cantMaterial;

        await _repo.Actualizar(material);
        return (MapDto(material), null);
    }

    public async Task<(bool eliminado, string? error)> Eliminar(int id)
    {
        var material = await _repo.ObtenerPorId(id);
        if (material == null)
            return (false, "NOT_FOUND");

        // No se puede eliminar si tiene reservas asociadas
        var reservaMateriales = await _repoReservaMaterial.ObtenerTodos();
        if (reservaMateriales.Any(rm => rm.Cod_Material == id))
            return (false, "No se puede eliminar un material con reservas asociadas");

        await _repo.Eliminar(material);
        return (true, null);
    }
}