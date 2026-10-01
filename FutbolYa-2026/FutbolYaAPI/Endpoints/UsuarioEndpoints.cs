using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Seguridad;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/usuarios: alta, baja, edición y reseteo de contraseña de cuentas. Capa fina:
// bind DTO → IUsuarioLogica → status code. Gestión de usuarios en general es solo
// Administrador, salvo el cambio de la propia contraseña.
public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios").WithTags("Usuarios");
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // ── GET /api/usuarios ──────────────────────────────────────────
        group.MapGet("/", async (IUsuarioLogica logica, bool incluirBajas = false) =>
        {
            try
            {
                var usuarios = await logica.ObtenerTodos(incluirBajas);
                return Results.Ok(usuarios);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");

        // ── GET /api/usuarios/{id} ─────────────────────────────────────
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
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");

        // ── POST /api/usuarios ─────────────────────────────────────────
        group.MapPost("/", async (UsuarioCreateDto dto, IUsuarioLogica logica, System.Security.Claims.ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error, codUsuarioInactivo) = await logica.Crear(dto, usuarioActual.CodUsuario());

                if (resultado is null)
                    return Results.Conflict(new { mensaje = error, cod_usuario_inactivo = codUsuarioInactivo });

                return Results.Created($"/api/usuarios/{resultado.Cod_Usuario}", resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");

        // ── PUT /api/usuarios/{id}/reactivar ────────────────────────────
        // Reactiva un usuario dado de baja en vez de crear un duplicado con el mismo DNI/correo.
        group.MapPut("/{id:int}/reactivar", async (int id, UsuarioCreateDto dto, IUsuarioLogica logica, System.Security.Claims.ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Reactivar(id, dto, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");

        // ── PUT /api/usuarios/{id} ─────────────────────────────────────
        group.MapPut("/{id:int}", async (int id, UsuarioUpdateDto dto, IUsuarioLogica logica, System.Security.Claims.ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.Actualizar(id, dto, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");

        // ── PATCH /api/usuarios/{id}/contrasena ────────────────────────
        // Cambio de contraseña propio: cualquier usuario autenticado, pero solo sobre sí mismo
        // (salvo que sea Administrador).
        group.MapPatch("/{id:int}/contrasena", async (int id, CambiarContraseñaDto dto, IUsuarioLogica logica, System.Security.Claims.ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var esAdmin = usuarioActual.IsInRole("Administrador");
                if (!esAdmin && usuarioActual.CodUsuario() != id)
                    return Results.Forbid();

                var (resultado, error) = await logica.CambiarContrasena(id, dto);

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization();

        // ── PATCH /api/usuarios/{id}/resetear-contrasena ─────────────────
        // El administrador dispara el envío de un código de verificación por correo; no recibe
        // ni define él mismo la contraseña temporal.
        group.MapPatch("/{id:int}/resetear-contrasena", async (int id, IUsuarioLogica logica, System.Security.Claims.ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (resultado, error) = await logica.IniciarReseteoPorAdmin(id, usuarioActual.CodUsuario());

                if (resultado is null)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");

        // ── DELETE /api/usuarios/{id} ──────────────────────────────────
        group.MapDelete("/{id:int}", async (int id, IUsuarioLogica logica, System.Security.Claims.ClaimsPrincipal usuarioActual) =>
        {
            try
            {
                var (eliminado, error) = await logica.Eliminar(id, usuarioActual.CodUsuario());

                if (!eliminado)
                    return error == "NOT_FOUND"
                        ? Results.NotFound(new { mensaje = "Usuario no encontrado", cod_usuario = id })
                        : Results.Conflict(new { mensaje = error });

                return Results.Ok(new { mensaje = "Usuario eliminado correctamente", cod_usuario = id });
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        }).RequireAuthorization("SoloAdministrador");
    }
}
