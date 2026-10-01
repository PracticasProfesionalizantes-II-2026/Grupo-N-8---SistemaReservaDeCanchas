namespace FutbolYaMVC.DTOs;

public record LoginRequest(
    string Correo,
    string Contrasena
);

public record LoginResponse(
    int Cod_Usuario,
    string Nombre,
    bool Rol,
    bool Cambiar_Contraseña,
    string Token
);

public record MensajeResponse(string Mensaje);

// Recuperación de contraseña por código enviado al correo.
public record SolicitarRecuperacionRequest(string Correo);

public record ConfirmarRecuperacionRequest(
    string Correo,
    string Codigo,
    string Contrasena_Nueva,
    string Contrasena_Nueva_Confirmacion
);

public record CambiarContrasenaRequest(
    string Contrasena_Actual,
    string Contrasena_Nueva,
    string Contrasena_Nueva_Confirmacion
);
