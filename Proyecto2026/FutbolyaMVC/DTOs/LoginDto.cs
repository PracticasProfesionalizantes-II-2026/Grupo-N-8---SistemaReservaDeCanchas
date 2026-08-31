using System.Text.Json.Serialization;

namespace FutbolyaMVC.DTOs;

public record LoginRequest(
    string Correo,
    string Contrasena
);

public record LoginResponse(
    [property: JsonPropertyName("cod_Usuario")] int Cod_Usuario,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("rol")] bool Rol,
    [property: JsonPropertyName("cambiar_Contraseña")] bool Cambiar_Contraseña
);