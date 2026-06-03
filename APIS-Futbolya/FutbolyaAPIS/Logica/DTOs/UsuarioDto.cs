namespace FutbolyaAPIS.Logica.DTOs;

public record UsuarioDto(
    int Cod_Usuario,
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    bool Rol,
    bool Cambiar_Contraseña
);

public record UsuarioCreateDto(
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    string Contraseña,
    bool Rol,
    bool Cambiar_Contraseña
);