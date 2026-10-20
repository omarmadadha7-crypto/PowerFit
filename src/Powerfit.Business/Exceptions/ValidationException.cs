using System;
using System.Collections.Generic;

namespace Powerfit.Business.Exceptions;

/// <summary>Invalid input. Carries per-field errors. Map to HTTP 400 / 422.</summary>
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string field, string error) : base(error)
    {
        Errors = new Dictionary<string, string[]>(1) { { field, new[] { error } } };
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
