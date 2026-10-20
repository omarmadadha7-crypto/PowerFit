using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Person
{
    public int PersonId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateOnly? BirthDate { get; set; }

    public string Role { get; set; } = null!;

    public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
