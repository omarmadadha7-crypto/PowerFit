namespace Powerfit.Business.Interfaces;

public interface IPasswordHasher
{
    /// <summary>Returns a self-describing hash (version, iterations, salt, hash) that fits Person.PasswordHash.</summary>
    string Hash(string password);

    bool Verify(string password, string passwordHash);

    /// <summary>True when the stored hash uses weaker parameters than the current ones.</summary>
    bool NeedsRehash(string passwordHash);
}
