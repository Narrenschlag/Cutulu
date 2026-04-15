namespace Cutulu.Encryption;

using System.Security.Cryptography;
using System.Text;
using System;

public static class SecureHash
{
    // ─────────────────────────────────────────────
    // CONFIG
    // ─────────────────────────────────────────────
    private const int SaltSize = 16;      // 128-bit salt
    private const int HashSize = 32;      // 256-bit hash
    private const int Iterations = 150_000;

    // ─────────────────────────────────────────────
    // PASSWORD HASHING
    // ─────────────────────────────────────────────

    /// <summary>
    /// Creates a salted PBKDF2 password hash.
    /// Format: salt:hash (hex)
    /// </summary>
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    /// <summary>
    /// Verifies a password against stored salt:hash
    /// </summary>
    public static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':', 2);

        if (parts.Length != 2)
            return false;

        var salt = Convert.FromHexString(parts[0]);
        var hash = Convert.FromHexString(parts[1]);

        var test = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return CryptographicOperations.FixedTimeEquals(hash, test);
    }

    // ─────────────────────────────────────────────
    // TOKEN / SESSION HASHING
    // ─────────────────────────────────────────────

    /// <summary>
    /// Creates a secure random token (for sessions, auth, etc.)
    /// </summary>
    public static string GenerateToken(int bytes = 32)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes));
    }

    /// <summary>
    /// Hashes a token for storage (optional extra security layer)
    /// Useful if you don't want raw tokens in DB.
    /// </summary>
    public static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);

        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ─────────────────────────────────────────────
    // GENERAL PURPOSE HASHING
    // ─────────────────────────────────────────────

    /// <summary>
    /// Fast non-cryptographic hash (NOT for passwords)
    /// </summary>
    public static string FastHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    // ─────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────

    public static byte[] RandomBytes(int size)
        => RandomNumberGenerator.GetBytes(size);
}