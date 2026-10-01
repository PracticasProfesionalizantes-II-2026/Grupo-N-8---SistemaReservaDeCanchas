namespace FutbolYaMVC.DTOs;

// ── Lectura ────────────────────────────────────────────────────────────

public record ReservaMaterialResponse(
    int Cod_Reserva_Mat,
    int Cod_Reserva,
    int Cod_Material,
    string Nombre_Material,
    int Cantidad
);

public record HorarioBloqueResponse(
    int Cod_Horario,
    TimeSpan Hora_Inicio,
    TimeSpan Hora_Fin
);

public record ReservaResponse(
    int Cod_Reserva,
    DateTime Fecha,
    DateTime FechaReserva,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    string Nombre_Cancha,
    int Cod_Usuario,
    string Nombre_Usuario,
    string Estado,
    int Duracion,
    List<HorarioBloqueResponse> Horarios,
    List<ReservaMaterialResponse>? Materiales
);

// ── Escritura ──────────────────────────────────────────────────────────

public record ReservaMaterialItemRequest(int Cod_Material, int Cantidad);

// Lo que llega desde el JS del navegador (sin Cod_Usuario: lo agrega el Controller desde la sesión).
public record ReservaCreateRequest(
    DateTime FechaReserva,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    List<int> Cod_Horarios,   // uno o más bloques contiguos
    List<ReservaMaterialItemRequest> Materiales
);

// Lo que el Service efectivamente envía a la API (con Cod_Usuario ya resuelto desde la sesión).
public record ReservaCreateApiRequest(
    DateTime FechaReserva,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    int Cod_Usuario,
    List<int> Cod_Horarios,
    List<ReservaMaterialItemRequest> Materiales
);

// ── ViewModel para la pantalla de listado ────────────────────────────────

public record ReservaIndexViewModel(
    List<ReservaResponse> Reservas,
    List<CanchaResponse> Canchas,
    List<HorarioResponse> Horarios,
    List<MaterialResponse> Materiales
);
