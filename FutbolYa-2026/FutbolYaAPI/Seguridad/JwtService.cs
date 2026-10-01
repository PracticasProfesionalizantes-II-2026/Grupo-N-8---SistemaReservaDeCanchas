using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FutbolYaAPI.Entidades;
using Microsoft.IdentityModel.Tokens;

namespace FutbolYaAPI.Seguridad;

public interface IJwtService
{
    string GenerarToken(Usuario usuario);
}

public class JwtOptions
{
    public string Secreto { get; set; } = string.Empty;
    public string Emisor { get; set; } = "FutbolYaAPI";
    public string Audiencia { get; set; } = "FutbolYaMVC";
    public int MinutosExpiracion { get; set; } = 480; // 8 horas: dura una jornada de trabajo del local
}

// Genera el JWT firmado que usa el cliente (FutbolYaMVC) para autenticarse contra la API.
// Solo emite el token; la validación de firma/expiración la hace el middleware de autenticación
// de Program.cs, que además revalida el token contra la base en cada request (OnTokenValidated)
// para detectar de inmediato un usuario dado de baja o con el rol cambiado.
public class JwtService : IJwtService
{
    private readonly JwtOptions _opciones;

    public JwtService(JwtOptions opciones)
    {
        _opciones = opciones;
    }

    public string GenerarToken(Usuario usuario)
    {
        var claves = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Secreto));
        var credenciales = new SigningCredentials(claves, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Cod_Usuario.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol ? "Administrador" : "Operador")
        };

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opciones.MinutosExpiracion),
            signingCredentials: credenciales
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
