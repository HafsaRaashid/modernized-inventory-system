using System.Security.Cryptography;

namespace InventoryTrackingSystem.Infrastructure.Auth;

/// <summary>
/// PBKDF2 (<see cref="Rfc2898DeriveBytes"/>) password hashing, BCL only —
/// no new NuGet dependency (see design.md Key Decisions). Uses OWASP's
/// current recommended minimum of 600,000 iterations with HMACSHA256 and a
/// 128-bit random salt per hash. The iteration count is stored alongside
/// each hash so it can be raised later without breaking existing hashes.
/// </summary>
public class PasswordHasherService
{
    private const int Iterations = 600_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Hashes <paramref name="password"/> with a fresh random salt, returning
    /// <c>"{iterations}.{saltBase64}.{hashBase64}"</c>.
    /// </summary>
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verifies <paramref name="password"/> against a hash previously produced
    /// by <see cref="Hash"/>, using a constant-time comparison to avoid
    /// timing attacks.
    /// </summary>
    public bool Verify(string password, string stored)
    {
        var parts = stored.Split('.', 3);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
