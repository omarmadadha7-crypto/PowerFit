using System;

namespace Powerfit.Business.Exceptions;

/// <summary>The request conflicts with the current state (duplicate e-mail, item in use...). Map to HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception inner) : base(message, inner) { }
}
