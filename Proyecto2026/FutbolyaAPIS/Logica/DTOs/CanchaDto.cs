namespace FutbolyaAPIS.Logica.DTOs;

public record CanchaDto(
    int Cod_Cancha,
    string Nombre,
    string Descripcion,
    bool Estado          // true = Disponible / false = En Mantenimiento
);

// Solo recibe descripcion, nombre y estado los asigna la logica
public record CanchaCreateDto(string Descripcion);

public record CanchaDescripcionUpdateDto(string Descripcion);

public record CanchaEstadoUpdateDto(bool Estado);