using System;
using System.Globalization;
using System.Security.Cryptography;
using Powerfit.Business.Interfaces;

namespace Powerfit.Business.Security;

/// <summary>
/// PBKDF2-HMAC-SHA512 (no extra NuGet package). Format: v1.{iterations}.{saltBase64}.{hashBase64}
/// (about 80 characters, fits NVARCHAR(255)).
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const string Version = "v1";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;
    private const int MaxAcceptedIterations = 5_000_000; // protects against crafted hashes

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashSize);

        return string.Join('.',
            Version,
            Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
            return false;

        if (!TryParse(passwordHash, out var iterations, out var salt, out var expected))
            return false;

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public bool NeedsRehash(string passwordHash)
    {
        if (!TryParse(passwordHash, out var iterations, out _, out var expected))
            return true;

        return iterations < Iterations || expected.Length < HashSize;
    }

    private static bool TryParse(string value, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        var parts = value.Split('.');
        if (parts.Length != 4 || parts[0] != Version)
            return false;

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            || iterations <= 0
            || iterations > MaxAcceptedIterations)
            return false;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length > 0;
    }
}
