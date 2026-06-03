namespace FutbolyaAPIS.Logica.DTOs;

public record ReservaDto(
    int Cod_Reserva,
    DateTime Fecha,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    int Cod_Usuario,
    int Duracion,
    int Cod_Horario
);

public record ReservaCreateDto(
    DateTime Fecha,
    string Dni_Cliente,
    string Telefono_Cliente,
    int Cod_Cancha,
    int Cod_Usuario,
    int Duracion,
    int Cod_Horario
);