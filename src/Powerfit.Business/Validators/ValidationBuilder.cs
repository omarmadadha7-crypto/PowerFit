using System;
using System.Collections.Generic;
using System.Linq;
using Powerfit.Business.Exceptions;

namespace Powerfit.Business.Validators;

/// <summary>Collects field errors and throws one ValidationException at the end.</summary>
public sealed class ValidationBuilder
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public bool HasErrors => _errors.Count > 0;

    public ValidationBuilder Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            _errors[field] = list;
        }

        list.Add(message);
        return this;
    }

    /// <summary>Adds the error when <paramref name="condition"/> is false.</summary>
    public ValidationBuilder Check(bool condition, string field, string message) =>
        condition ? this : Add(field, message);

    public ValidationBuilder Required(string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Add(field, $"{field} is required.");

        return value.Trim().Length > maxLength
            ? Add(field, $"{field} must be at most {maxLength} characters.")
            : this;
    }

    public ValidationBuilder Optional(string field, string? value, int maxLength) =>
        value is not null && value.Trim().Length > maxLength
            ? Add(field, $"{field} must be at most {maxLength} characters.")
            : this;

    public ValidationBuilder IntRange(string field, int? value, int min, int max, bool required = false)
    {
        if (value is null)
            return required ? Add(field, $"{field} is required.") : this;

        return value < min || value > max
            ? Add(field, $"{field} must be between {min} and {max}.")
            : this;
    }

    public ValidationBuilder DecimalRange(string field, decimal? value, decimal min, decimal max, bool required = false)
    {
        if (value is null)
            return required ? Add(field, $"{field} is required.") : this;

        return value < min || value > max
            ? Add(field, $"{field} must be between {min} and {max}.")
            : this;
    }

    public ValidationBuilder OneOf(string field, string? value, IReadOnlyCollection<string> allowed, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return required ? Add(field, $"{field} is required.") : this;

        return allowed.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase)
            ? this
            : Add(field, $"{field} must be one of: {string.Join(", ", allowed)}.");
    }

    public void ThrowIfInvalid()
    {
        if (HasErrors)
            throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }
}
