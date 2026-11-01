using FZScout.Logs;
using FZScout.Scouting;
using FZScout.Site;

namespace FZScout.Tests.Scouting;

public class CharacterScoutTests
{
    const string Befallen = "You have entered Befallen.";
    const string Crushbone = "You have entered Clan Crushbone.";
    const string Kill = "You have slain a skeleton!";
    const string Experience = "This land is saturated with unstable magic, improving the experiences of all who venture here.";
    const string Tribute = "This land is saturated with unstable magic, enhancing the potency of any offerings made here.";
    const string DailyReset = "Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources.";
    const string Camping = "It will take you about 30 seconds to prepare your camp.";
    const string CampAbandoned = "You abandon your preparations to camp.";

    static readonly Character Bob = new("Bob", Server.Frostreaver);
    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    // Like Frostreaver's: no tribute.
    static readonly ServerLists Lists = new(
        new HashSet<BonusKey> { new("experience"), new("aa"), new("loot"), BonusKey.None },
        new HashSet<string> { "Befallen", "Crushbone", "Greater Faydark" });

    readonly CharacterScout _scout = new(Bob);

    [Fact]
    public void BonusMessageIsReported()
    {
        Log(0, Befallen);

        Assert.Equal([Report("Befallen", "experience")], Log(10, Experience));
    }

    [Fact]
    public void ZoneIsReportedByItsSiteName()
    {
        Log(0, Crushbone);

        Assert.Equal([Report("Crushbone", "experience")], Log(10, Experience));
    }

    [Fact]
    public void NoBonusFiveMinutesAfterEnteringIsReportedAsNone()
    {
        Log(0, Befallen);

        Assert.Empty(Tick(299));
        Assert.Equal([ReportNone("Befallen")], Tick(300));
    }

    [Fact]
    public void NoneIsReportedOnlyOnce()
    {
        Log(0, Befallen);
        Tick(300);

        Assert.Empty(Tick(600));
    }

    [Fact]
    public void NoBonusFiveSecondsAfterAKillIsReportedAsNone()
    {
        Log(0, Befallen);
        Log(20, Kill);

        Assert.Empty(Tick(24));
        Assert.Equal([ReportNone("Befallen")], Tick(25));
        Assert.Empty(Tick(300));
    }

    [Fact]
    public void KillByOwnPetCountsAsAKill()
    {
        Log(0, Befallen);
        Log(20, "A wolf was slain by Bob`s pet!");

        Assert.Equal([ReportNone("Befallen")], Tick(25));
    }

    [Fact]
    public void KillByOwnWarderCountsAsAKill()
    {
        Log(0, Befallen);
        Log(20, "A gnoll pup was slain by Bob`s warder!");

        Assert.Equal([ReportNone("Befallen")], Tick(25));
    }

    [Fact]
    public void KillByAnotherPlayersPetIsNotAKill()
    {
        Log(0, Befallen);
        Log(20, "A wolf was slain by Alice`s pet!");

        Assert.Empty(Tick(25));
    }

    [Fact]
    public void LaterKillsDoNotPushTheKillTimerBack()
    {
        Log(0, Befallen);
        Log(20, Kill);
        Log(23, Kill);

        Assert.Equal([ReportNone("Befallen")], Tick(25));
    }

    [Fact]
    public void KillsAfterABonusDoNothing()
    {
        Log(0, Befallen);
        Log(20, Kill);
        Log(20, Experience);
        Log(40, Kill);

        Assert.Empty(Tick(300));
    }

    [Fact]
    public void BonusAfterReportingNoneIsStillReported()
    {
        // A kill doesn't always trigger the message; sometimes it comes minutes later.
        Log(0, Befallen);
        Log(20, Kill);
        Tick(25);

        Assert.Equal([Report("Befallen", "experience")], Log(200, Experience));
    }

    [Fact]
    public void EnteringAnotherZoneRestartsTheIdleTimer()
    {
        Log(0, Befallen);
        Log(100, Crushbone);

        Assert.Empty(Tick(300));
        Assert.Equal([ReportNone("Crushbone")], Tick(400));
    }

    [Fact]
    public void EnteringAnotherZoneCancelsTheKillTimer()
    {
        Log(0, Befallen);
        Log(20, Kill);
        Log(22, Crushbone);

        Assert.Empty(Tick(30));
    }

    [Fact]
    public void ReEnteringTheSameZoneStartsANewVisit()
    {
        Log(0, Befallen);
        Log(10, Experience);
        Log(100, Befallen);

        Assert.Equal([ReportNone("Befallen")], Tick(400));
    }

    [Fact]
    public void DailyResetRestartsTheIdleTimer()
    {
        Log(0, Befallen);
        Log(10, Experience);
        Log(1000, DailyReset);

        Assert.Empty(Tick(1299));
        Assert.Equal([ReportNone("Befallen")], Tick(1300));
    }

    [Fact]
    public void DailyResetRearmsKills()
    {
        Log(0, Befallen);
        Log(10, Experience);
        Log(1000, DailyReset);
        Log(1010, Kill);

        Assert.Equal([ReportNone("Befallen")], Tick(1015));
    }

    [Fact]
    public void CampingPausesTheIdleTimer()
    {
        Log(0, Befallen);
        Log(200, Camping);
        Log(205, "It will take about 25 more seconds to prepare your camp.");

        Assert.Empty(Tick(10_000));
    }

    [Fact]
    public void AbandoningCampResumesTheIdleTimerWhereItLeftOff()
    {
        Log(0, Befallen);
        Log(200, Camping);
        Log(205, Camping);
        Log(210, CampAbandoned);

        Assert.Empty(Tick(309));
        Assert.Equal([ReportNone("Befallen")], Tick(310));
    }

    [Fact]
    public void DailyResetWhileCampingWaitsForTheCampToBeAbandoned()
    {
        Log(0, Befallen);
        Log(10, Experience);
        Log(100, Camping);
        Log(101, DailyReset);

        Assert.Empty(Tick(1000));
        Log(1000, CampAbandoned);
        Assert.Equal([ReportNone("Befallen")], Tick(1300));
    }

    [Fact]
    public void EnteringAZoneIsLogged()
    {
        Assert.Equal([new LogInfoAction($"Bob (Frostreaver) entered The Greater Faydark, waiting until {Start.AddMinutes(5).ToLocalTime():HH:mm:ss} for a bonus message.")], Log(0, "You have entered The Greater Faydark."));
    }

    [Fact]
    public void BonusBeforeAnyZoneIsIgnored()
    {
        Assert.Empty(Log(0, Experience));
        Assert.Empty(Tick(1000));
    }

    [Fact]
    public void BonusTypeNotOnTheServersListIsIgnored()
    {
        Log(0, Befallen);

        Assert.Empty(Log(10, Tribute));
    }

    [Fact]
    public void UnknownZoneWithNoBonusIsNotAnError()
    {
        Assert.Equal(
            [new LogInfoAction("Bob (Frostreaver) entered The Halls of Honor.")],
            Log(0, "You have entered The Halls of Honor."));
        Assert.Empty(Log(10, Kill));
        Assert.Empty(Tick(1000));
    }

    [Fact]
    public void BonusInAnUnknownZoneIsAnError()
    {
        Log(0, "You have entered The Halls of Honor.");

        Assert.Equal(
            [new LogErrorAction($"Bob (Frostreaver) saw a 'experience' bonus in 'The Halls of Honor', which isn't on the Frostreaver zones list. Please report it at {CharacterScout.ReportIssuesUrl}")],
            Log(10, Experience));
    }

    [Fact]
    public void ZoneEnteredWithoutListsIsResolvedWhenTheyLoad()
    {
        Assert.Equal([new LogInfoAction($"Bob (Frostreaver) entered Befallen, waiting until {Start.AddMinutes(5).ToLocalTime():HH:mm:ss} for a bonus message.")], LogWithoutLists(0, Befallen));
        Assert.Empty(this._scout.Tick(Start.AddSeconds(60), null));

        Assert.Empty(Tick(61));
        Assert.Equal(new KnownZone("Befallen"), this._scout.CurrentZone);
        Assert.Equal([ReportNone("Befallen")], Tick(300));
    }

    [Fact]
    public void BonusAfterTheListsLoadIsReported()
    {
        LogWithoutLists(0, Befallen);

        Assert.Equal([Report("Befallen", "experience")], Log(10, Experience));
    }

    [Fact]
    public void ZoneEnteredWithoutListsThatTurnsOutUnknownStopsWaiting()
    {
        LogWithoutLists(0, "You have entered The Halls of Honor.");

        Assert.Empty(Tick(60));
        Assert.Equal(new UnknownZone("The Halls of Honor"), this._scout.CurrentZone);
        Assert.Empty(Tick(1000));
    }

    [Fact]
    public void ListsThatNeverLoadMeanNoneIsntReported()
    {
        LogWithoutLists(0, Befallen);

        Assert.Equal(
            [new LogErrorAction("Bob (Frostreaver) was in 'Befallen', but the Frostreaver zones list still isn't loaded, so 'none' wasn't reported.")],
            this._scout.Tick(Start.AddSeconds(300), null));
        Assert.Empty(this._scout.Tick(Start.AddSeconds(1000), null));
    }

    [Fact]
    public void BonusWhileListsAreMissingIsAnError()
    {
        LogWithoutLists(0, Befallen);

        Assert.Equal(
            [new LogErrorAction("Bob (Frostreaver) saw a 'experience' bonus in 'Befallen', but the Frostreaver zones list isn't loaded, so it wasn't reported.")],
            LogWithoutLists(10, Experience));
    }

    [Fact]
    public void DailyResetDropsPendingReports()
    {
        Assert.Equal([new DropPendingReportsAction(Bob)], Log(0, DailyReset));
    }

    [Fact]
    public void ZoneThatNeverHasABonusWaitsForNothing()
    {
        Log(0, "You have entered The Plane of Knowledge.");
        Assert.Empty(Log(10, Kill));
        Assert.Empty(Tick(1000));
    }

    [Fact]
    public void BonusInAZoneThatNeverHasOneIsAnError()
    {
        Log(0, "You have entered The Plane of Knowledge.");

        Assert.Equal(
            [new LogErrorAction($"Bob (Frostreaver) saw a 'experience' bonus in 'The Plane of Knowledge', which never has a bonus. Please report it at {CharacterScout.ReportIssuesUrl}")],
            Log(10, Experience));
    }

    [Fact]
    public void AreasInsideAZoneDontChangeIt()
    {
        Log(0, Befallen);
        Log(100, "You have entered an area where levitation effects do not function.");

        Assert.Equal([ReportNone("Befallen")], Tick(300));
    }

    [Fact]
    public void ArenaAreasInsideAZoneDontChangeIt()
    {
        Log(0, Befallen);
        Log(100, "You have entered an Arena (PvP) area.");

        Assert.Equal([ReportNone("Befallen")], Tick(300));
    }

    static ReportBonusAction Report(string zone, string bonus) => new(Bob, new BonusReport(Server.Frostreaver, zone, new BonusKey(bonus)));

    static ReportNoBonusAction ReportNone(string zone) => new(Bob, zone);

    IReadOnlyList<ScoutAction> Log(int seconds, string message)
    {
        var logEvent = LogEvent.Parse($"[Thu Oct 29 12:00:00 2026] {message}");
        if (logEvent is null)
        {
            return [];
        }

        return this._scout.Handle(logEvent, Start.AddSeconds(seconds), Lists);
    }

    IReadOnlyList<ScoutAction> Tick(int seconds) => this._scout.Tick(Start.AddSeconds(seconds), Lists);

    IReadOnlyList<ScoutAction> LogWithoutLists(int seconds, string message)
    {
        return this._scout.Handle(LogEvent.Parse(message)!, Start.AddSeconds(seconds), null);
    }
}
