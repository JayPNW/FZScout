using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

public class AppStatusTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    readonly FakeTimeProvider _timeProvider = new(Start);
    readonly AppStatus _status;

    public AppStatusTests()
    {
        this._timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        this._status = new AppStatus(this._timeProvider);
    }

    [Fact]
    public void ShowsTheGemWhenNothingIsGoingOn()
    {
        Assert.Equal((StatusIcon.Gem, "FZScout", null), Shown());
    }

    [Fact]
    public void ShowsTheHourglassUntilWaitingForABonusMessageEnds()
    {
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.Equal((StatusIcon.WaitingForBonusMessage, "FZScout: waiting until 12:05:00 for a bonus message", Start.AddMinutes(5)), Shown());
    }

    [Fact]
    public void ShowsTheGemOnceWaitingForABonusMessageEnds()
    {
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        this._timeProvider.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal((StatusIcon.Gem, "FZScout", null), Shown());
    }

    (StatusIcon Icon, string Description, DateTimeOffset? ChangesByItselfAt) Shown() => (this._status.Icon, this._status.Description, this._status.ChangesByItselfAt);
}
