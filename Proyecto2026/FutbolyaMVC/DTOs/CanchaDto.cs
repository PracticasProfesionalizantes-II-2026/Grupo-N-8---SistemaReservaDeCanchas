using System.Text.Json.Serialization;

namespace FutbolyaMVC.DTOs;

public record CanchaResponse(
    [property: JsonPropertyName("cod_Cancha")] int Cod_Cancha,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("descripcion")] string Descripcion,
    [property: JsonPropertyName("estado")] bool Estado   // true = Disponible / false = En Mantenimiento
);

public record CanchaCreateRequest(
    string Descripcion
);

public record CanchaUpdateRequest(
    string Descripcion
);