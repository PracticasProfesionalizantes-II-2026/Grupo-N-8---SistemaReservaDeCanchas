

namespace FutbolyaAPIS.Logica.Dtos;


public record HorarioDisponibleDto(int id, TimeSpan HoraIncio, TimeSpan HoraFin, bool Activo);

public record HorarioDisponibleCreateDto(TimeSpan HoraIncio, TimeSpan HoraFin);



