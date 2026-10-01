using System.Text.Json;
using FutbolYaAPI.Entidades;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IAuditoriaLogica
{
    /// <summary>
    /// Registra un alta, modificación o baja para el historial de auditoría.
    /// valorAnterior/valorNuevo se serializan como JSON; se dejan en null según corresponda
    /// (Alta no tiene anterior, Baja no tiene nuevo). Se llama dentro de la misma transacción
    /// que el cambio de negocio que audita, para que un rollback también revierta el registro
    /// de auditoría.
    /// </summary>
    Task Registrar(int codUsuario, string entidad, int codEntidad, AccionAuditoria accion, object? valorAnterior, object? valorNuevo);
    Task<IEnumerable<AuditoriaDto>> ObtenerTodos(string? entidad, int? codUsuario);
}

// Escribe y consulta el historial de auditoría (altas, modificaciones, bajas) de las demás
// entidades del sistema. Se ubica entre las clases *Logica que disparan los cambios y
// AuditoriaRepository; no valida reglas de negocio, solo persiste y expone lo que le pasan.
public class AuditoriaLogica : IAuditoriaLogica
{
    private readonly IAuditoriaRepository _repo;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public AuditoriaLogica(IAuditoriaRepository repo)
    {
        _repo = repo;
    }

    public async Task Registrar(int codUsuario, string entidad, int codEntidad, AccionAuditoria accion, object? valorAnterior, object? valorNuevo)
    {
        var registro = new Auditoria
        {
            Cod_Usuario         = codUsuario,
            Entidad_Afectada    = entidad,
            Cod_Entidad_Afectada = codEntidad,
            Accion              = accion,
            Fecha_Hora          = DateTime.Now,
            Valor_Anterior      = valorAnterior is null ? null : JsonSerializer.Serialize(valorAnterior, JsonOpts),
            Valor_Nuevo         = valorNuevo is null ? null : JsonSerializer.Serialize(valorNuevo, JsonOpts)
        };

        await _repo.Agregar(registro);
    }

    public async Task<IEnumerable<AuditoriaDto>> ObtenerTodos(string? entidad, int? codUsuario)
    {
        var registros = await _repo.ObtenerTodos(entidad, codUsuario);
        return registros.Select(a => new AuditoriaDto(
            a.Cod_Auditoria,
            a.Cod_Usuario,
            $"{a.Usuario?.Nombre} {a.Usuario?.Apellido}".Trim(),
            a.Fecha_Hora,
            a.Entidad_Afectada,
            a.Cod_Entidad_Afectada,
            a.Accion.ToString(),
            a.Valor_Anterior,
            a.Valor_Nuevo
        ));
    }
}
