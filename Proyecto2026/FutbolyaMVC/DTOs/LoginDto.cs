namespace FutbolyaMVC.DTOs;

public record LoginRequest(
    string Correo,
    string Contrasena
);

public record LoginResponse(
    int Cod_Usuario,
    string Nombre,
    bool Rol,
    bool Cambiar_Contraseña
);