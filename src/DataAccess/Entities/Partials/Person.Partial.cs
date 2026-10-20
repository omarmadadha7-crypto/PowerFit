using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities;

public partial class Person
{
    [NotMapped] public string FullName => $"{FirstName} {LastName}".Trim();
    [NotMapped] public bool IsAdmin => Role == UserRoles.Admin;
    [NotMapped] public bool IsRegularUser => Role == UserRoles.User;
    [NotMapped] public int? Age => GetAge();

    public int? GetAge(DateOnly? today = null)
    {
        if (BirthDate is not { } birth) return null;

        var now = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = now.Year - birth.Year;
        if (birth > now.AddYears(-age)) age--;
        return age < 0 ? null : age;
    }

    /// <summary>Trims names and lower-cases the e-mail (the DB CHECKs use LEN(TRIM(..)) > 0).</summary>
    public void Normalize()
    {
        FirstName = FirstName?.Trim() ?? string.Empty;
        LastName = LastName?.Trim() ?? string.Empty;
        Email = Email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>Builds Person (Role=User) + its User row as one graph.</summary>
    public static Person CreateUser(
        string firstName, string lastName, string email, string passwordHash, DateOnly? birthDate = null)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            BirthDate = birthDate,
            Role = UserRoles.User
        };
        person.Normalize();
        person.Users.Add(new User { Person = person });
        return person;
    }

    /// <summary>Builds Person (Role=Admin) + its Admin row as one graph.</summary>
    public static Person CreateAdmin(
        string firstName, string lastName, string email, string passwordHash, DateOnly? birthDate = null)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            BirthDate = birthDate,
            Role = UserRoles.Admin
        };
        person.Normalize();
        person.Admins.Add(new Admin { Person = person });
        return person;
    }
}
