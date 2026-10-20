using System;
using System.Text.RegularExpressions;
using Powerfit.Business.DTOs.Auth;

namespace Powerfit.Business.Validators;

public static class AuthValidator
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void Validate(RegisterRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.Required("firstName", r.FirstName, 50)
         .Required("lastName", r.LastName, 50);

        ValidateEmail(v, r.Email);
        ValidatePassword(v, "password", r.Password);
        ValidateBirthDate(v, r.BirthDate);
        v.ThrowIfInvalid();
    }

    public static void Validate(LoginRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.Required("email", r.Email, 254)
         .Required("password", r.Password, 128);
        v.ThrowIfInvalid();
    }

    public static void Validate(ChangePasswordRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.Required("currentPassword", r.CurrentPassword, 128);
        ValidatePassword(v, "newPassword", r.NewPassword);
        v.ThrowIfInvalid();
    }

    public static void ValidateBirthDate(ValidationBuilder v, DateOnly? birthDate)
    {
        if (birthDate is not { } b) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        v.Check(b >= new DateOnly(1900, 1, 1) && b < today, "birthDate", "birthDate must be a past date after 1900-01-01.");
    }

    private static void ValidateEmail(ValidationBuilder v, string? email)
    {
        v.Required("email", email, 254);
        if (!string.IsNullOrWhiteSpace(email))
            v.Check(EmailRegex.IsMatch(email.Trim()), "email", "email is not a valid e-mail address.");
    }

    private static void ValidatePassword(ValidationBuilder v, string field, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            v.Add(field, $"{field} is required.");
            return;
        }

        v.Check(password.Length >= 8, field, $"{field} must be at least 8 characters.");
        v.Check(password.Length <= 128, field, $"{field} must be at most 128 characters.");

        var hasLetter = false;
        var hasDigit = false;
        foreach (var c in password)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
        }

        v.Check(hasLetter && hasDigit, field, $"{field} must contain at least one letter and one digit.");
    }
}
