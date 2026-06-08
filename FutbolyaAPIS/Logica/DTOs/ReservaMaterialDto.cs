namespace FutbolyaAPIS.Logica.DTOs;

// Lectura — se usa en las respuestas de Reserva
public record ReservaMaterialDto(
    int Cod_Reserva_Mat,
    int Cod_Reserva,
    int Cod_Material,
    string Nombre_Material,
    int Cantidad
);

// Escritura — se usa al crear/agregar materiales
public record ReservaMaterialItemDto(
    int Cod_Material,
    int Cantidad
);