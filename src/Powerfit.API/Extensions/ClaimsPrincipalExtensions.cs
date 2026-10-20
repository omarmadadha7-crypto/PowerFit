using System.Globalization;
using System.Security.Claims;
using Powerfit.Business.Exceptions;

namespace Powerfit.API.Extensions;

/// <summary>
/// Reads the ids that TokenService writes into the JWT: "sub" = PersonId, "uid" = UserId or AdminId.
/// (JwtBearer runs with MapInboundClaims = false, so the short claim names are kept.)
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Person.PersonID of the caller.</summary>
    public static int GetPersonId(this ClaimsPrincipal principal) => ReadId(principal, "sub");

    /// <summary>User.UserID for users, Admin.AdminID for admins.</summary>
    public static int GetAccountId(this ClaimsPrincipal principal) => ReadId(principal, "uid");

    /// <summary>Use inside endpoints protected by the UserOnly policy.</summary>
    public static int GetUserId(this ClaimsPrincipal principal) => principal.GetAccountId();

    /// <summary>Use inside endpoints protected by the AdminOnly policy.</summary>
    public static int GetAdminId(this ClaimsPrincipal principal) => principal.GetAccountId();

    private static int ReadId(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            throw new UnauthorizedException("The token is missing a required claim.");

        return id;
    }
}
