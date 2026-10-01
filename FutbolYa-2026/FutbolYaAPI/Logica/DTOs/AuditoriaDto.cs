namespace FutbolYaAPI.Logica.DTOs;

public record AuditoriaDto(
    int Cod_Auditoria,
    int Cod_Usuario,
    string Nombre_Usuario,
    DateTime Fecha_Hora,
    string Entidad_Afectada,
    int Cod_Entidad_Afectada,
    string Accion,
    string? Valor_Anterior,
    string? Valor_Nuevo
);
