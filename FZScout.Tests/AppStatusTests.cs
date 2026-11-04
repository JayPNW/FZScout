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
        this._status = new AppStatus(this._timeProvider) { IsScouting = true };
    }

    [Fact]
    public void ShowsTheGreyGemUntilScoutingStarts()
    {
        this._status.IsScouting = false;

        Assert.Equal((StatusIcon.NotScouting, "FZScout: not scouting yet", null), Shown());
    }

    [Fact]
    public void ShowsTheGreyGemWithAProblemBadgeOnceTheAppHasStopped()
    {
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        this._status.HasStopped = true;

        Assert.Equal((StatusIcon.Stopped, "FZScout has stopped. Its window shows why.", null), Shown());
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

    [Fact]
    public void ShowsAProblemBadgeOnTheGemAfterAProblem()
    {
        this._status.HadProblemSinceLastSuccess = true;

        Assert.Equal((StatusIcon.GemAfterProblem, "FZScout: something went wrong; its window shows what", null), Shown());
    }

    [Fact]
    public void ShowsAProblemBadgeOnTheHourglassAfterAProblem()
    {
        this._status.HadProblemSinceLastSuccess = true;
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.Equal(
            (StatusIcon.WaitingForBonusMessageAfterProblem, "FZScout: waiting until 12:05:00 for a bonus message, and something went wrong; its window shows what", Start.AddMinutes(5)),
            Shown());
    }

    [Fact]
    public void DescriptionsFitInATrayIconTooltip()
    {
        this._status.HadProblemSinceLastSuccess = true;
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.InRange(this._status.Description.Length, 1, 127);
    }

    (StatusIcon Icon, string Description, DateTimeOffset? ChangesByItselfAt) Shown() => (this._status.Icon, this._status.Description, this._status.ChangesByItselfAt);
}
