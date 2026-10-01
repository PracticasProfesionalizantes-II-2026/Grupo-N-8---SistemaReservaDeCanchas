using FutbolYaAPI.Logica;
using FutbolYaAPI.Logica.DTOs;
using Microsoft.AspNetCore.RateLimiting;

namespace FutbolYaAPI.Endpoints;

// Mapea /api/auth: login y recuperación de contraseña, anónimo. Cada acción está limitada
// por IP (RequireRateLimiting) para frenar fuerza bruta y abuso del envío de correos.
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Autenticación").AllowAnonymous();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // ── POST /api/auth/login ───────────────────────────────────────
        group.MapPost("/login", async (LoginDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Contrasena))
                    return Results.BadRequest(new { mensaje = "Correo y contraseña son obligatorios" });

                var (resultado, _) = await logica.Login(dto);
                if (resultado is null)
                    return Results.Unauthorized();

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        })
        // 5 intentos por minuto por IP.
        .RequireRateLimiting("login");

        // ── POST /api/auth/solicitar-recuperacion ───────────────────────
        // Flujo "¿Olvidé mi contraseña?" desde el login.
        group.MapPost("/solicitar-recuperacion", async (SolicitarRecuperacionDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Correo))
                    return Results.BadRequest(new { mensaje = "El correo es obligatorio" });

                var (resultado, _) = await logica.SolicitarRecuperacion(dto);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        })
        // 3 intentos cada 10 minutos por IP (envía un correo real).
        .RequireRateLimiting("solicitar-recuperacion");

        // ── POST /api/auth/confirmar-recuperacion ───────────────────────
        group.MapPost("/confirmar-recuperacion", async (ConfirmarRecuperacionDto dto, IUsuarioLogica logica) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Codigo) ||
                    string.IsNullOrWhiteSpace(dto.Contrasena_Nueva) || string.IsNullOrWhiteSpace(dto.Contrasena_Nueva_Confirmacion))
                    return Results.BadRequest(new { mensaje = "Todos los campos son obligatorios" });

                var (resultado, error) = await logica.ConfirmarRecuperacion(dto);
                if (resultado is null)
                    return Results.BadRequest(new { mensaje = error });

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return ex.ProblemaInterno(logger);
            }
        })
        // 5 intentos cada 10 minutos por IP (protege el código de 6 dígitos contra fuerza bruta).
        .RequireRateLimiting("confirmar-recuperacion");
    }
}
