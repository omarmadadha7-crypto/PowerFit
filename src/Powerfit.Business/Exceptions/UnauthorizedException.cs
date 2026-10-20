using System;

namespace Powerfit.Business.Exceptions;

/// <summary>Wrong credentials or unknown account. Map to HTTP 401.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
