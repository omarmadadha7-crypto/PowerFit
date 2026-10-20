using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Settings;

namespace Powerfit.Business.Security;

/// <summary>
/// Creates JWTs (HS256). Claims written: sub (PersonId), uid (UserId/AdminId), email, name, role, jti.
/// Register as Singleton.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;

        if (string.IsNullOrWhiteSpace(_settings.Key) || Encoding.UTF8.GetByteCount(_settings.Key) < 32)
            throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 bytes.");

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
            SecurityAlgorithms.HmacSha256);
    }

    public TokenResult CreateToken(TokenSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expires,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = subject.PersonId.ToString(CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.Email] = subject.Email,
                [JwtRegisteredClaimNames.Name] = subject.FullName,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                ["role"] = subject.Role,
                ["uid"] = subject.AccountId.ToString(CultureInfo.InvariantCulture)
            }
        };

        return new TokenResult(_handler.CreateToken(descriptor), expires);
    }
}
