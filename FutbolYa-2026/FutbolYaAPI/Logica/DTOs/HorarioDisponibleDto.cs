namespace FutbolYaAPI.Logica.DTOs;

public record HorarioDisponibleDto(
    int Cod_Horario,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    bool Activo
);

public record HorarioActivoUpdateDto(bool Activo);
