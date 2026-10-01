namespace FutbolYaAPI.Logica.DTOs;

// ── Lectura ────────────────────────────────────────────────────────────

public record ReservaMaterialDto(
    int Cod_Reserva_Mat,
    int Cod_Reserva,
    int Cod_Material,
    string Nombre_Material,
    int Cantidad
);

public record HorarioBloqueDto(
    int Cod_Horario,
    TimeSpan Hora_Inicio,
    TimeSpan Hora_Fin
);

public record ReservaDto(
    int Cod_Reserva,
    DateTime Fecha,           // fecha de creación, la asigna el sistema
    DateTime FechaReserva,    // fecha para la que se reserva
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    string Nombre_Cancha,
    int Cod_Usuario,
    string Nombre_Usuario,
    string Estado,
    int Duracion,             // cantidad de bloques de 1 hora (calculado)
    IEnumerable<HorarioBloqueDto> Horarios,
    IEnumerable<ReservaMaterialDto>? Materiales = null
);

// ── Escritura ──────────────────────────────────────────────────────────

public record ReservaMaterialItemDto(
    int Cod_Material,
    int Cantidad
);

public record ReservaCreateDto(
    DateTime FechaReserva,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    int Cod_Usuario,
    IEnumerable<int> Cod_Horarios,   // uno o más bloques contiguos, en vez de un único horario
    IEnumerable<ReservaMaterialItemDto> Materiales
);

