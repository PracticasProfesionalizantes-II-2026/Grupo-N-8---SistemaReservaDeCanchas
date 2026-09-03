using System.Text.Json.Serialization;

namespace FutbolyaMVC.DTOs;

public record MaterialResponse(
    [property: JsonPropertyName("cod_Material")] int Cod_Material,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("cant_Material")] int Cant_Material
);

public record MaterialCreateRequest(
    string Nombre,
    int Cant_Material
);
