using System;

namespace Powerfit.Business.DTOs.Auth;

public record AuthResponse(string Token, DateTime ExpiresAtUtc, AuthUserDto User);

/// <summary>
/// UserId is set for normal users, AdminId for admins. The token carries the same ids
/// (claims: sub = PersonId, uid = UserId or AdminId, role = User/Admin).
/// </summary>
public record AuthUserDto(
    int PersonId,
    int? UserId,
    int? AdminId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Role,
    bool ProfileCompleted);
