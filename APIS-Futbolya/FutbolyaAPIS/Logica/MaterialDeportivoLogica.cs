using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IMaterialDeportivoLogica
{
    Task<IEnumerable<MaterialDeportivoDto>> ObtenerTodos();
    Task<MaterialDeportivoDto?> ObtenerPorId(int id);
    Task<int> Crear(MaterialDeportivoCreateDto dto);
    Task<bool> Actualizar(int id, MaterialDeportivoCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class MaterialDeportivoLogica : IMaterialDeportivoLogica
{
    private readonly IMaterialDeportivoRepository _repo;

    public MaterialDeportivoLogica(IMaterialDeportivoRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<MaterialDeportivoDto>> ObtenerTodos()
    {
        var materiales = await _repo.ObtenerTodos();
        return materiales.Select(m => new MaterialDeportivoDto(
            m.Cod_Material,
            m.Nombre,
            m.Cant_Material
        ));
    }

    public async Task<MaterialDeportivoDto?> ObtenerPorId(int id)
    {
        var m = await _repo.ObtenerPorId(id);
        if (m == null) return null;

        return new MaterialDeportivoDto(
            m.Cod_Material,
            m.Nombre,
            m.Cant_Material
        );
    }

    public async Task<int> Crear(MaterialDeportivoCreateDto dto)
    {
        var material = new Material_Deportivo
        {
            Nombre = dto.Nombre,
            Cant_Material = dto.Cant_Material
        };
        await _repo.Agregar(material);
        return material.Cod_Material;
    }

    public async Task<bool> Actualizar(int id, MaterialDeportivoCreateDto dto)
    {
        var material = await _repo.ObtenerPorId(id);
        if (material == null) return false;

        material.Nombre = dto.Nombre;
        material.Cant_Material = dto.Cant_Material;

        await _repo.Actualizar(material);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var material = await _repo.ObtenerPorId(id);
        if (material == null) return false;

        await _repo.Eliminar(material);
        return true;
    }
}