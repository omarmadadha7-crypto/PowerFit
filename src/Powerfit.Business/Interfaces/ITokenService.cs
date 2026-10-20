using System;

namespace Powerfit.Business.Interfaces;

/// <param name="PersonId">Person.PersonID.</param>
/// <param name="AccountId">User.UserID for users, Admin.AdminID for admins.</param>
public sealed record TokenSubject(int PersonId, int AccountId, string Email, string FullName, string Role);

public sealed record TokenResult(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    /// <summary>Creates a signed JWT with the claims: sub, uid, email, name, role, jti.</summary>
    TokenResult CreateToken(TokenSubject subject);
}
