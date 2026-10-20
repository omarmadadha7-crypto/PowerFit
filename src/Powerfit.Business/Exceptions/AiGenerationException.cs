using System;

namespace Powerfit.Business.Exceptions;

/// <summary>
/// The AI provider failed, timed out or returned an invalid plan.
/// The attempt is already stored in AIGenerationLog. Map to HTTP 502 / 504.
/// </summary>
public class AiGenerationException : Exception
{
    public bool IsTimeout { get; }

    public AiGenerationException(string message, bool isTimeout = false) : base(message)
    {
        IsTimeout = isTimeout;
    }
}
