namespace FutbolYaAPI.Logica.DTOs;

public record UsuarioDto(
    int Cod_Usuario,
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    bool Rol,               // true = Administrador / false = Operador
    bool Cambiar_Contraseña,
    bool Activo
);

public record LoginResponseDto(
    int Cod_Usuario,
    string Nombre,
    bool Rol,               // true = Administrador / false = Operador
    bool Cambiar_Contraseña,
    string Token
);

public record MensajeResponseDto(string Mensaje);

// ── Escritura ──────────────────────────────────────────────────────────

public record UsuarioCreateDto(
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    string Contraseña,
    bool Rol                // true = Administrador / false = Operador
);

public record UsuarioUpdateDto(
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    bool Rol
);

public record CambiarContraseñaDto(
    string Contrasena_Actual,
    string Contrasena_Nueva,
    string Contrasena_Nueva_Confirmacion
);

public record LoginDto(
    string Correo,
    string Contrasena
);

// Recuperación de contraseña por código enviado al correo: valida que quien solicita
// el cambio tiene acceso a esa casilla, en vez de resetear a una contraseña temporal.
public record SolicitarRecuperacionDto(string Correo);

public record ConfirmarRecuperacionDto(
    string Correo,
    string Codigo,
    string Contrasena_Nueva,
    string Contrasena_Nueva_Confirmacion
);
