using System.Text.Json;

namespace FutbolYaMVC.Infraestructura;

/// <summary>
/// Opciones de (de)serialización compartidas por todos los Services al hablar con la API.
/// La API serializa con JsonNamingPolicy.SnakeCaseLower (ej. HoraInicio -> hora_inicio), así que
/// hay que usar la misma policy acá para deserializar; PropertyNameCaseInsensitive por sí solo
/// no alcanza, porque no inserta ni quita guiones bajos, solo ignora mayúsculas/minúsculas
/// (esto pasó desapercibido mientras cada nombre de propiedad multi-palabra ya tuviera su
/// guion bajo escrito a mano, ej. Cod_Usuario; en propiedades como HoraInicio fallaba en silencio
/// y quedaban en su valor por defecto).
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };
}
