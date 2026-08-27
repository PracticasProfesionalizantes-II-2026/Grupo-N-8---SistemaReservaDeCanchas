using FutbolyaAPIS.Logica;
using FutbolyaAPIS.Logica.DTOs;

namespace FutbolyaAPIS.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios").WithTags("Usuarios");
        // ── GET /api/usuarios ──────────────────────────────────────────
        //Lista todos los usuarios
        group.MapGet("/", async (IUsuarioLogica logica) =>
        {
            try
            {
                var usuarios = await logica.ObtenerTodos();
                return Results.Ok(usuarios);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── GET /api/usuarios/{id} ─────────────────────────────────────
        //Obtiene un usuario por su ID
        group.MapGet("/{id:int}", async (int id, IUsuarioLogica logica) =>
        {
            try
            {
                var usuario = await logica.ObtenerPorId(id);
                if (usuario is null)
                    return Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id });

                return Results.Ok(usuario);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/usuarios ─────────────────────────────────────────
        //Crea un nuevo usuario
        group.MapPost("/", async (UsuarioCreateDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                //validación de campos obligatorios
                if (string.IsNullOrWhiteSpace(dto.Nombre)    ||
                    string.IsNullOrWhiteSpace(dto.Apellido)  ||
                    string.IsNullOrWhiteSpace(dto.Dni)       ||
                    string.IsNullOrWhiteSpace(dto.Correo)    ||
                    string.IsNullOrWhiteSpace(dto.Contraseña))
                    return Results.BadRequest(new { mensaje = "Todos los campos son obligatorios" });

                var (resultado, error) = await logica.Crear(dto);

                // Si el resultado es nulo, significa que hubo un error (por ejemplo, conflicto de DNI o correo)
                if (resultado is null)
                    return Results.Conflict(new { mensaje = error });

                return Results.Created($"/api/usuarios/{resultado.Cod_Usuario}", resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PUT /api/usuarios/{id} ─────────────────────────────────────
        //Actualiza un usuario existente
        group.MapPut("/{id:int}", async (int id, UsuarioUpdateDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                //validamos campos obligatorios
                if (string.IsNullOrWhiteSpace(dto.Nombre)   ||
                    string.IsNullOrWhiteSpace(dto.Apellido) ||
                    string.IsNullOrWhiteSpace(dto.Dni)      ||
                    string.IsNullOrWhiteSpace(dto.Correo))
                    return Results.BadRequest(new { mensaje = "Todos los campos son obligatorios" });

                var (resultado, error) = await logica.Actualizar(id, dto);

                // Si el resultado es nulo, significa que hubo un error (por ejemplo, usuario no encontrado o conflicto de DNI/correo)
                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/usuarios/{id}/contrasena ────────────────────────
        //Actualiza la contraseña de un usuario existente(boton de su perfil)
        group.MapPatch("/{id:int}/contrasena", async (int id, CambiarContraseñaDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Contrasena_Nueva) ||
                    string.IsNullOrWhiteSpace(dto.Contrasena_Nueva_Confirmacion))
                    return Results.BadRequest(new { mensaje = "La nueva contraseña no puede estar vacía" });

                if (dto.Contrasena_Nueva != dto.Contrasena_Nueva_Confirmacion)
                    return Results.BadRequest(new { mensaje = "La nueva contraseña y su confirmación no coinciden" });

                var (resultado, error) = await logica.CambiarContrasena(id, dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── PATCH /api/usuarios/{id}/resetear-contrasena ─────────────────
        //Resetea la contraseña de un usuario existente (boton de administrador)
        group.MapPatch("/{id:int}/resetear-contrasena", async (int id, ResetearContraseñaDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Contrasena_Temporal))
                    return Results.BadRequest(new { mensaje = "La contraseña temporal no puede estar vacía" });
                
                var (resultado, error) = await logica.ResetearContrasena(id, dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── POST /api/auth/login ───────────────────────────────────────
        //Autentica un usuario y devuelve un token JWT
        group.MapPost("/api/auth/login", async (LoginDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Correo) ||
                    string.IsNullOrWhiteSpace(dto.Contrasena))
                    return Results.BadRequest(new { mensaje = "Correo y contraseña son obligatorios" });

                var (resultado, error) = await logica.Login(dto);

                if (resultado is null)
                    return Results.Unauthorized();

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ── DELETE /api/usuarios/{id} ──────────────────────────────────
        //Elimina un usuario existente
        group.MapDelete("/{id:int}", async (int id, IUsuarioLogica logica) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id);

                // Si no se pudo eliminar, devolvemos un error adecuado
                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Usuario eliminado correctamente", cod_usuario = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}