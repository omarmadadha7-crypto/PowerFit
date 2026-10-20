using System;
using System.Collections.Concurrent;
using System.Threading;
using Powerfit.Business.Interfaces;

namespace Powerfit.Business.Security;

/// <summary>
/// In-memory lockout: 5 failures inside 15 minutes lock the key for 15 minutes. Register as Singleton.
/// Per instance only: with several API instances put the counters in a shared store (Redis / SQL).
/// Keyed by e-mail (also for unknown e-mails, so it reveals nothing); the trade-off is that someone can
/// lock another person's sign-in for 15 minutes, which is why the lock is short.
/// </summary>
public sealed class LoginAttemptTracker : ILoginAttemptTracker
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private sealed class Entry
    {
        public int Failures;
        public DateTime FirstFailureUtc;
        public DateTime? LockedUntilUtc;
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _time;
    private int _calls;

    public LoginAttemptTracker() : this(TimeProvider.System) { }

    public LoginAttemptTracker(TimeProvider time)
    {
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    public bool IsLockedOut(string key, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        if (!_entries.TryGetValue(key, out var entry)) return false;

        var now = _time.GetUtcNow().UtcDateTime;
        lock (entry)
        {
            if (entry.LockedUntilUtc is { } until && until > now)
            {
                retryAfter = until - now;
                return true;
            }
        }

        return false;
    }

    public void RecordFailure(string key)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var entry = _entries.GetOrAdd(key, _ => new Entry { FirstFailureUtc = now });

        lock (entry)
        {
            if (entry.LockedUntilUtc is { } until && until > now) return; // already locked

            // Old window or an expired lock: start counting again.
            if (now - entry.FirstFailureUtc > Window || entry.LockedUntilUtc is not null)
            {
                entry.Failures = 0;
                entry.FirstFailureUtc = now;
                entry.LockedUntilUtc = null;
            }

            entry.Failures++;
            if (entry.Failures >= MaxFailures)
                entry.LockedUntilUtc = now + LockDuration;
        }

        if (Interlocked.Increment(ref _calls) % 256 == 0) Sweep(now);
    }

    public void Reset(string key) => _entries.TryRemove(key, out _);

    /// <summary>Removes entries whose window and lock are over (keeps memory bounded).</summary>
    private void Sweep(DateTime now)
    {
        foreach (var (key, entry) in _entries)
        {
            bool stale;
            lock (entry)
            {
                var lockOver = entry.LockedUntilUtc is null || entry.LockedUntilUtc <= now;
                stale = lockOver && now - entry.FirstFailureUtc > Window;
            }

            if (stale) _entries.TryRemove(key, out _);
        }
    }
}
