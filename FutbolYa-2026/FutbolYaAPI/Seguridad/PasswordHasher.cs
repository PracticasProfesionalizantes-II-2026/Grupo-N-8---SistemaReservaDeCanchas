using System.Security.Cryptography;

namespace FutbolYaAPI.Seguridad;

/// <summary>
/// Hashing y verificación de contraseñas con PBKDF2-SHA256 (Rfc2898DeriveBytes), auto-contenido
/// (sin dependencias externas). Lo usan Logica (alta/cambio de contraseña) y el sembrado del
/// Administrador inicial. El hash guarda las iteraciones usadas junto con el salt y el resultado
/// (formato "iter.salt.hash"), de modo que la cantidad de iteraciones pueda subir en el futuro
/// sin invalidar los hashes ya almacenados con un valor anterior.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;       // 128 bits
    private const int KeySize = 32;        // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public static string Hash(string contraseñaPlana)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contraseñaPlana, salt, Iterations, Algoritmo, KeySize);

        // Formato almacenado: iteraciones.salt.hash (todo en Base64 excepto las iteraciones)
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string contraseñaPlana, string hashAlmacenado)
    {
        var partes = hashAlmacenado.Split('.', 3);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
            return false;

        var salt = Convert.FromBase64String(partes[1]);
        var hashEsperado = Convert.FromBase64String(partes[2]);

        var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(contraseñaPlana, salt, iteraciones, Algoritmo, hashEsperado.Length);
        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}
