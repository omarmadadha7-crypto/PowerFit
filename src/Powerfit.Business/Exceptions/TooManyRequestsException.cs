using System;

namespace Powerfit.Business.Exceptions;

/// <summary>A usage limit was reached (login lockout, daily AI quota...). Map to HTTP 429.</summary>
public class TooManyRequestsException : Exception
{
    /// <summary>When the caller may try again (sent as the Retry-After header). Null = unknown.</summary>
    public TimeSpan? RetryAfter { get; }

    public TooManyRequestsException(string message, TimeSpan? retryAfter = null) : base(message)
    {
        RetryAfter = retryAfter;
    }
}
