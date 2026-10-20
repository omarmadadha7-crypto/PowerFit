using System;

namespace DataAccess.Entities;

public partial class Admin
{
    public Admin()
    {
        Role = UserRoles.Admin; // must match Person.Role (composite FK + CHECK)
    }
}
