namespace FutbolYaMVC.DTOs;

public record HorarioResponse(
    int Cod_Horario,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    bool Activo
);
