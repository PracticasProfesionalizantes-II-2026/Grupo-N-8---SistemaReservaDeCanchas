namespace FutbolyaAPIS.Logica.DTOs;

public record ReservaMaterialDto(
    int Cod_Reserva_Mat,
    int Cod_Reserva,
    int Cod_Material,
    int Cantidad
);

public record ReservaMaterialCreateDto(
    int Cod_Reserva,
    int Cod_Material,
    int Cantidad
);