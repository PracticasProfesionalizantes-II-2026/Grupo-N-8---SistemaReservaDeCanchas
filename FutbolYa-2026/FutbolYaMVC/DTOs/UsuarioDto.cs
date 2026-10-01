namespace FutbolYaMVC.DTOs;

public record UsuarioResponse(
    int Cod_Usuario,
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    bool Rol,
    bool Cambiar_Contraseña,
    bool Activo
);

public record UsuarioCreateRequest(
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    string Contraseña,
    bool Rol
);

public record UsuarioUpdateRequest(
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    bool Rol
);
