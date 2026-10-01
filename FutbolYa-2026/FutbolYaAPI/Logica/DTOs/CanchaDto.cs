using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Logica.DTOs;

public record CanchaDto(
    int Cod_Cancha,
    string Nombre,
    string Descripcion,
    string Estado,          // "Disponible" | "Mantenimiento" | "Baja"
    List<int> Cod_Horarios  // bloques horarios del catálogo global que ofrece esta cancha
);

// Solo recibe descripcion, nombre y estado los asigna la logica
public record CanchaCreateDto(string Descripcion, List<int> Cod_Horarios);

public record CanchaDescripcionUpdateDto(string Descripcion);

public record CanchaEstadoUpdateDto(EstadoCancha Estado);

public record CanchaHorariosUpdateDto(List<int> Cod_Horarios);
