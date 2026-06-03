namespace FutbolyaAPIS.Logica.DTOs;

public record CanchaDto(
    int Cod_Cancha,
    string Nombre,
    string Descripcion,
    bool Estado
);

public record CanchaCreateDto(
    string Nombre,
    string Descripcion,
    bool Estado
);