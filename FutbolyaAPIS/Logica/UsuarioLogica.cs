using FutbolyaAPIS.Entidades;
using FutbolyaAPIS.Logica.DTOs;
using FutbolyaAPIS.Repositorios;

namespace FutbolyaAPIS.Logica;

public interface IUsuarioLogica
{
    Task<IEnumerable<UsuarioDto>> ObtenerTodos();
    Task<UsuarioDto?> ObtenerPorId(int id);
    Task<int> Crear(UsuarioCreateDto dto);
    Task<bool> Actualizar(int id, UsuarioCreateDto dto);
    Task<bool> Eliminar(int id);
}

public class UsuarioLogica : IUsuarioLogica
{
    private readonly IUsuarioRepository _repo;

    public UsuarioLogica(IUsuarioRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<UsuarioDto>> ObtenerTodos()
    {
        var usuarios = await _repo.ObtenerTodos();
        return usuarios.Select(u => new UsuarioDto(
            u.Cod_Usuario,
            u.Nombre,
            u.Apellido,
            u.Dni,
            u.Direccion,
            u.Correo,
            u.Rol,
            u.Cambiar_Contraseña
        ));
    }

    public async Task<UsuarioDto?> ObtenerPorId(int id)
    {
        var u = await _repo.ObtenerPorId(id);
        if (u == null) return null;

        return new UsuarioDto(
            u.Cod_Usuario,
            u.Nombre,
            u.Apellido,
            u.Dni,
            u.Direccion,
            u.Correo,
            u.Rol,
            u.Cambiar_Contraseña
        );
    }

    public async Task<int> Crear(UsuarioCreateDto dto)
    {
        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Dni = dto.Dni,
            Direccion = dto.Direccion,
            Correo = dto.Correo,
            Contraseña = dto.Contraseña,
            Rol = dto.Rol,
            Cambiar_Contraseña = dto.Cambiar_Contraseña,
        };
        await _repo.Agregar(usuario);
        return usuario.Cod_Usuario;
    }

    public async Task<bool> Actualizar(int id, UsuarioCreateDto dto)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null) return false;

        usuario.Nombre = dto.Nombre;
        usuario.Apellido = dto.Apellido;
        usuario.Dni = dto.Dni;
        usuario.Direccion = dto.Direccion;
        usuario.Correo = dto.Correo;
        usuario.Contraseña = dto.Contraseña;
        usuario.Rol = dto.Rol;
        usuario.Cambiar_Contraseña = dto.Cambiar_Contraseña;

        await _repo.Actualizar(usuario);
        return true;
    }

    public async Task<bool> Eliminar(int id)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null) return false;

        await _repo.Eliminar(usuario);
        return true;
    }
}