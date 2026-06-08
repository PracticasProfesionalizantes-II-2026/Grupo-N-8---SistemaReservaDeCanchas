namespace FutbolyaAPIS.Logica.DTOs;

public record MaterialDeportivoDto(
    int Cod_Material,
    string Nombre,
    int Cant_Material
);

public record MaterialDeportivoCreateDto(
    string Nombre,
    int Cant_Material
);