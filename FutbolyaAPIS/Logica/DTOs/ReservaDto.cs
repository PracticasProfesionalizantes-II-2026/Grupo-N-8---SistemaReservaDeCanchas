namespace FutbolyaAPIS.Logica.DTOs;

// ── Lectura ────────────────────────────────────────────────────────────

public record ReservaMaterialDto(
    int Cod_Reserva_Mat,
    int Cod_Reserva,
    int Cod_Material,
    string Nombre_Material,
    int Cantidad
);

public record ReservaDto(
    int Cod_Reserva,
    DateTime Fecha,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    string Nombre_Cancha,
    int Cod_Usuario,
    string Nombre_Usuario,
    int Duracion,
    int Cod_Horario,
    TimeSpan Hora_Inicio,
    TimeSpan Hora_Fin,
    IEnumerable<ReservaMaterialDto>? Materiales = null
);

// ── Escritura ──────────────────────────────────────────────────────────

public record ReservaMaterialItemDto(
    int Cod_Material,
    int Cantidad
);

public record ReservaCreateDto(
    DateTime Fecha,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    int Cod_Usuario,
    int Duracion,
    int Cod_Horario,
    IEnumerable<ReservaMaterialItemDto> Materiales
);

public record ReservaUpdateDto(
    DateTime Fecha,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    int Cod_Horario,
    int Duracion,
    IEnumerable<ReservaMaterialItemDto> Materiales
);

public record ReservaMaterialAddDto(
    int Cod_Material,
    int Cantidad
);