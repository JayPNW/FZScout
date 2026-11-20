using FZScout.Site;
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
        this._status = new AppStatus(this._timeProvider) { IsScouting = true, IsEverQuestRunning = true };
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
    public void ShowsEverQuestLoggingIsOffAboveEverythingButStopping()
    {
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);
        this._status.HadProblemSinceLastSuccess = true;

        this._status.IsEverQuestLoggingEnabled = false;

        Assert.Equal((ScoutingStatus.EverQuestLoggingDisabled, "FZScout: EverQuest logging is off. Type `/log` on in EverQuest.", Start.AddMinutes(5)), Shown());
    }

    [Fact]
    public void IsNotScoutingWhileEverQuestIsNotRunning()
    {
        this._status.IsEverQuestRunning = false;

        Assert.Equal((ScoutingStatus.NotScouting, "FZScout: waiting for EverQuest to start", null), Shown());
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

        Assert.Equal((ScoutingStatus.ScoutingAfterProblem, "FZScout: hit a problem. Open the main window for details.", null), Shown());
    }

    [Fact]
    public void IsWaitingForBonusMessageAfterProblemWhenThereWasAProblem()
    {
        this._status.HadProblemSinceLastSuccess = true;
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.Equal(
            (ScoutingStatus.WaitingForBonusMessageAfterProblem, "FZScout: waiting until 12:05:00 for a bonus message; hit a problem. Open the main window for details.", Start.AddMinutes(5)),
            Shown());
    }

    [Fact]
    public void SaysToEnterANewApiKeyWhenTheProblemIsThatItWasRejected()
    {
        this._status.HadProblemSinceLastSuccess = true;
        this._status.IsApiKeyRejected = true;

        Assert.Equal((ScoutingStatus.ScoutingAfterProblem, "FZScout: API Key rejected; enter a new one in settings", null), Shown());
    }

    [Fact]
    public void ShowsAnUpdateWasDownloadedWhenNothingElseIsGoingOn()
    {
        this._status.IsUpdateDownloaded = true;

        Assert.Equal((ScoutingStatus.ScoutingWithUpdateDownloaded, "FZScout: update downloaded; it installs when FZScout exits", null), Shown());
    }

    [Fact]
    public void ShowsOtherStatusesInsteadOfADownloadedUpdate()
    {
        this._status.IsUpdateDownloaded = true;
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);

        Assert.Equal((ScoutingStatus.WaitingForBonusMessage, "FZScout: waiting until 12:05:00 for a bonus message", Start.AddMinutes(5)), Shown());
    }

    [Fact]
    public void ShowsWhatTheSiteShowsForTheCurrentZone()
    {
        this._status.BonusShownOnSiteForCurrentZone = new BonusShownOnSite(Server.Fangbreaker, "Crushbone", new BonusKey("loot"));

        Assert.Equal((ScoutingStatus.Scouting, "FZScout\nCrushbone: 'loot' on fangbreaker.zone", null), Shown());
    }

    [Fact]
    public void DescriptionsFitInATrayIconTooltip()
    {
        this._status.HadProblemSinceLastSuccess = true;
        this._status.IsApiKeyRejected = true;
        this._status.WaitingForBonusMessageUntil = Start.AddMinutes(5);
        this._status.BonusShownOnSiteForCurrentZone = new BonusShownOnSite(Server.Fangbreaker, "Temple of Cazic-Thule", new BonusKey("unconfirmed"));

        Assert.InRange(this._status.Description.Length, 1, 127);
    }

    (ScoutingStatus ScoutingStatus, string Description, DateTimeOffset? ChangesByItselfAt) Shown() => (this._status.ScoutingStatus, this._status.Description, this._status.ChangesByItselfAt);
}
