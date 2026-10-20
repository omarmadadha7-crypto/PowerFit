using Powerfit.Business.Security;
using Xunit;

namespace Powerfit.Tests.Business;

public class LoginAttemptTrackerTests
{
    private sealed class FakeTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static void Fail(LoginAttemptTracker tracker, string key, int times)
    {
        for (var i = 0; i < times; i++) tracker.RecordFailure(key);
    }

    [Fact]
    public void Five_failures_lock_the_key()
    {
        var tracker = new LoginAttemptTracker(new FakeTime());

        Fail(tracker, "a@b.com", LoginAttemptTracker.MaxFailures - 1);
        Assert.False(tracker.IsLockedOut("a@b.com", out _));

        tracker.RecordFailure("a@b.com");

        Assert.True(tracker.IsLockedOut("a@b.com", out var retryAfter));
        Assert.Equal(LoginAttemptTracker.LockDuration, retryAfter);
    }

    [Fact]
    public void Other_keys_are_not_affected()
    {
        var tracker = new LoginAttemptTracker(new FakeTime());

        Fail(tracker, "a@b.com", 10);

        Assert.False(tracker.IsLockedOut("other@b.com", out _));
    }

    [Fact]
    public void The_lock_expires()
    {
        var time = new FakeTime();
        var tracker = new LoginAttemptTracker(time);
        Fail(tracker, "a@b.com", 5);

        time.Now += LoginAttemptTracker.LockDuration + TimeSpan.FromSeconds(1);

        Assert.False(tracker.IsLockedOut("a@b.com", out _));
    }

    [Fact]
    public void A_successful_sign_in_resets_the_counter()
    {
        var tracker = new LoginAttemptTracker(new FakeTime());
        Fail(tracker, "a@b.com", 4);

        tracker.Reset("a@b.com");
        Fail(tracker, "a@b.com", 4);

        Assert.False(tracker.IsLockedOut("a@b.com", out _));
    }

    [Fact]
    public void Failures_outside_the_window_do_not_add_up()
    {
        var time = new FakeTime();
        var tracker = new LoginAttemptTracker(time);
        Fail(tracker, "a@b.com", 4);

        time.Now += LoginAttemptTracker.Window + TimeSpan.FromMinutes(1);
        Fail(tracker, "a@b.com", 4);

        Assert.False(tracker.IsLockedOut("a@b.com", out _));
    }
}
