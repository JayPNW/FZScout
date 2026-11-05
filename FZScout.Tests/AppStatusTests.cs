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
    public void IsNotScoutingUntilScoutingStarts()
    {
        this._status.IsScouting = false;

        Assert.Equal((ScoutingStatus.NotScouting, "FZScout: not scouting yet", null), Shown());
    }

    [Fact]
    public void IsStoppedOnceTheAppHasStopped()
    {
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        this._status.HasStopped = true;

        Assert.Equal((ScoutingStatus.Stopped, "FZScout has stopped. Its window shows why.", null), Shown());
    }

    [Fact]
    public void IsScoutingWhenNothingElseIsGoingOn()
    {
        Assert.Equal((ScoutingStatus.Scouting, "FZScout", null), Shown());
    }

    [Fact]
    public void IsWaitingForBonusMessageUntilItEnds()
    {
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.Equal((ScoutingStatus.WaitingForBonusMessage, "FZScout: waiting until 12:05:00 for a bonus message", Start.AddMinutes(5)), Shown());
    }

    [Fact]
    public void IsScoutingOnceWaitingForABonusMessageEnds()
    {
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        this._timeProvider.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal((ScoutingStatus.Scouting, "FZScout", null), Shown());
    }

    [Fact]
    public void IsScoutingAfterProblemWhenThereWasAProblem()
    {
        this._status.HadProblemSinceLastSuccess = true;

        Assert.Equal((ScoutingStatus.ScoutingAfterProblem, "FZScout: something went wrong; its window shows what", null), Shown());
    }

    [Fact]
    public void IsWaitingForBonusMessageAfterProblemWhenThereWasAProblem()
    {
        this._status.HadProblemSinceLastSuccess = true;
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.Equal(
            (ScoutingStatus.WaitingForBonusMessageAfterProblem, "FZScout: waiting until 12:05:00 for a bonus message, and something went wrong; its window shows what", Start.AddMinutes(5)),
            Shown());
    }

    [Fact]
    public void DescriptionsFitInATrayIconTooltip()
    {
        this._status.HadProblemSinceLastSuccess = true;
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.InRange(this._status.Description.Length, 1, 127);
    }

    (ScoutingStatus ScoutingStatus, string Description, DateTimeOffset? ChangesByItselfAt) Shown() => (this._status.ScoutingStatus, this._status.Description, this._status.ChangesByItselfAt);
}
