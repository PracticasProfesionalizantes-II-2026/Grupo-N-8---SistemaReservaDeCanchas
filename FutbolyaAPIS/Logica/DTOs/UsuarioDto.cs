namespace FutbolyaAPIS.Logica.DTOs;

public record UsuarioDto(
    int Cod_Usuario,
    string Nombre,
    string Apellido,
    string Dni,
    string Direccion,
    string Correo,
    bool Rol,               // true = Administrador / false = Operador
    bool Cambiar_Contraseña
);

public record LoginResponseDto(
    int Cod_Usuario,
    string Nombre,
    bool Rol,               // true = Administrador / false = Operador
    bool Cambiar_Contraseña
);

public record CambiarContraseñaResponseDto(
    string Mensaje,
    bool Cambiar_Contraseña
);

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