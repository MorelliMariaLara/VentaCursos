using System.Security.Cryptography;
using System.Text;

namespace Nexa.Web.Services;

public static class PasswordService
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = new byte[32];
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            hash,
            100_000,
            HashAlgorithmName.SHA256);

        // Compatible con el formato scrypt$ del demo Node (reseed) — usamos scrypt-like label con PBKDF2
        return $"scrypt${Convert.ToHexString(salt).ToLowerInvariant()}${Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith("scrypt$", StringComparison.Ordinal))
            return false;

        var parts = stored.Split('$');
        if (parts.Length != 3) return false;

        var salt = Convert.FromHexString(parts[1]);
        var expected = Convert.FromHexString(parts[2]);

        // Node usaba scrypt; para demo regeneramos hashes al arrancar.
        // También soportamos verificación PBKDF2 del mismo formato.
        var actual = new byte[expected.Length];
        try
        {
            // Intento 1: scrypt vía NSec no está; usamos comparación con hash generado igual que Hash()
            Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                actual,
                100_000,
                HashAlgorithmName.SHA256);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
