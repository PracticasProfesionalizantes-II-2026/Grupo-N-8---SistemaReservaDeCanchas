namespace FutbolYaAPI.Logica.DTOs;

public record MaterialDeportivoDto(
    int Cod_Material,
    string Nombre,
    int Cant_Material,
    bool Activo
);

public record MaterialDeportivoCreateDto(
    string Nombre,
    int Cant_Material
);

// Mismo motivo que ProductoUpdateDto: el stock no se edita por PUT.
public record MaterialDeportivoUpdateDto(
    string Nombre
);