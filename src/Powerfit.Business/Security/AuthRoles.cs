using DataAccess.Entities;

namespace Powerfit.Business.Security;

/// <summary>Role names as they appear in the JWT "role" claim (same values as Person.Role).</summary>
public static class AuthRoles
{
    public const string User = UserRoles.User;
    public const string Admin = UserRoles.Admin;
}
