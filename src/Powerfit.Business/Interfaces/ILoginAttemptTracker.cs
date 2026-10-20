using System;

namespace Powerfit.Business.Interfaces;

/// <summary>Counts failed sign-ins per key (normalised e-mail) and locks the key for a while.</summary>
public interface ILoginAttemptTracker
{
    bool IsLockedOut(string key, out TimeSpan retryAfter);
    void RecordFailure(string key);
    void Reset(string key);
}
