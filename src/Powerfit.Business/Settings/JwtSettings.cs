namespace Powerfit.Business.Settings;

/// <summary>Bound from the "Jwt" section of appsettings.json.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PowerFit";
    public string Audience { get; set; } = "PowerFit.Clients";

    /// <summary>Signing key, at least 32 characters. Keep it in user-secrets / environment variables.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
