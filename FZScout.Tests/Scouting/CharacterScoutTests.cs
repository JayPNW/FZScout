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
    const string Chat = "You say, 'hello'";
    const string CampAbandoned = "You abandon your preparations to camp.";

    static readonly Character Bob = new("Bob", Server.Frostreaver);
    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    // Like Frostreaver's: no tribute.
    static readonly ServerLists Lists = new(
        new HashSet<BonusKey> { new("experience"), new("aa"), new("loot"), BonusKey.None },
        new HashSet<string> { "Befallen", "Crushbone", "Greater Faydark" });

    static readonly EverQuestLogWatcher.EverQuestLogLineParser Parser = new(new EverQuestLogWatcher.Character(Bob.Name, Bob.Server.Name));

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

        Assert.Empty(Log(299, Chat));
        Assert.Equal([ReportNone("Befallen")], Log(300, Chat));
    }

    [Fact]
    public void NoneIsReportedOnlyOnce()
    {
        Log(0, Befallen);
        Log(300, Chat);

        Assert.Empty(Log(600, Chat));
    }

    [Fact]
    public void NoBonusFiveSecondsAfterAKillIsReportedAsNone()
    {
        Log(0, Befallen);
        Log(20, Kill);

        Assert.Empty(Log(24, Chat));
        Assert.Equal([ReportNone("Befallen")], Log(25, Chat));
        Assert.Empty(Log(300, Chat));
    }

    [Fact]
    public void KillByOwnPetCountsAsAKill()
    {
        Log(0, Befallen);
        Log(20, "A wolf was slain by Bob`s pet!");

        Assert.Equal([ReportNone("Befallen")], Log(25, Chat));
    }

    [Fact]
    public void KillByOwnWarderCountsAsAKill()
    {
        Log(0, Befallen);
        Log(20, "A gnoll pup was slain by Bob`s warder!");

        Assert.Equal([ReportNone("Befallen")], Log(25, Chat));
    }

    [Fact]
    public void KillByAnotherPlayersPetIsNotAKill()
    {
        Log(0, Befallen);
        Log(20, "A wolf was slain by Alice`s pet!");

        Assert.Empty(Log(25, Chat));
    }

    [Fact]
    public void LaterKillsDoNotPushTheKillTimerBack()
    {
        Log(0, Befallen);
        Log(20, Kill);
        Log(23, Kill);

        Assert.Equal([ReportNone("Befallen")], Log(25, Chat));
    }

    [Fact]
    public void KillsAfterABonusDoNothing()
    {
        Log(0, Befallen);
        Log(20, Kill);
        Log(20, Experience);
        Log(40, Kill);

        Assert.Empty(Log(300, Chat));
    }

    [Fact]
    public void BonusAfterReportingNoneIsStillReported()
    {
        // A kill doesn't always trigger the message; sometimes it comes minutes later.
        Log(0, Befallen);
        Log(20, Kill);
        Log(25, Chat);

        Assert.Equal([Report("Befallen", "experience")], Log(200, Experience));
    }

    [Fact]
    public void EnteringAnotherZoneRestartsTheIdleTimer()
    {
        Log(0, Befallen);
        Log(100, Crushbone);

        Assert.Empty(Log(300, Chat));
        Assert.Equal([ReportNone("Crushbone")], Log(400, Chat));
    }

    [Fact]
    public void EnteringAnotherZoneCancelsTheKillTimer()
    {
        Log(0, Befallen);
        Log(20, Kill);
        Log(22, Crushbone);

        Assert.Empty(Log(30, Chat));
    }

    [Fact]
    public void ReEnteringTheSameZoneStartsANewVisit()
    {
        Log(0, Befallen);
        Log(10, Experience);
        Log(100, Befallen);

        Assert.Equal([ReportNone("Befallen")], Log(400, Chat));
    }

    [Fact]
    public void DailyResetRestartsTheIdleTimer()
    {
        Log(0, Befallen);
        Log(10, Experience);
        Log(1000, DailyReset);

        Assert.Empty(Log(1299, Chat));
        Assert.Equal([ReportNone("Befallen")], Log(1300, Chat));
    }

    [Fact]
    public void DailyResetRearmsKills()
    {
        Log(0, Befallen);
        Log(10, Experience);
        Log(1000, DailyReset);
        Log(1010, Kill);

        Assert.Equal([ReportNone("Befallen")], Log(1015, Chat));
    }

    [Fact]
    public void CampingStopsTheIdleTimer()
    {
        Log(0, Befallen);
        Log(200, Camping);
        Log(205, "It will take about 25 more seconds to prepare your camp.");

        Assert.Empty(Log(10_000, Chat));
    }

    [Fact]
    public void AbandoningCampDoesNotRestartTheIdleTimer()
    {
        Log(0, Befallen);
        Log(200, Camping);
        Log(210, CampAbandoned);

        Assert.Empty(Log(10_000, Chat));
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
        Assert.Empty(Log(1000, Chat));
    }

    [Fact]
    public void BonusTypeNotOnTheServersListIsAnError()
    {
        Log(0, Befallen);

        Assert.Equal(
            [new LogErrorAction($"Bob (Frostreaver) saw a 'tribute' bonus in 'Befallen', which isn't on the Frostreaver bonus types list. Please report it at {CharacterScout.ReportIssuesUrl}")],
            Log(10, Tribute));
    }

    [Fact]
    public void BonusTypeNotOnTheServersListMeansNoneIsntReported()
    {
        Log(0, Befallen);
        Log(20, Kill);
        Log(21, Tribute);

        Assert.Empty(Log(25, Chat));
        Assert.Empty(Log(300, Chat));
    }

    [Fact]
    public void UnknownZoneWithNoBonusIsNotAnError()
    {
        Assert.Equal(
            [new LogInfoAction("Bob (Frostreaver) entered The Plane of Knowledge.")],
            Log(0, "You have entered The Plane of Knowledge."));
        Assert.Empty(Log(10, Kill));
        Assert.Empty(Log(1000, Chat));
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
        Assert.Empty(LogWithoutLists(60, Chat));

        Assert.Empty(Log(61, Chat));
        Assert.Equal(new KnownZone("Befallen"), this._scout.CurrentZone);
        Assert.Equal([ReportNone("Befallen")], Log(300, Chat));
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

        Assert.Empty(Log(60, Chat));
        Assert.Equal(new UnknownZone("The Halls of Honor"), this._scout.CurrentZone);
        Assert.Empty(Log(1000, Chat));
    }

    [Fact]
    public void ListsThatNeverLoadMeanNoneIsntReported()
    {
        LogWithoutLists(0, Befallen);

        Assert.Equal(
            [new LogErrorAction("Bob (Frostreaver) was in 'Befallen', but the Frostreaver zones list still isn't loaded, so 'none' wasn't reported.")],
            LogWithoutLists(300, Chat));
        Assert.Empty(LogWithoutLists(1000, Chat));
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
    public void AreasInsideAZoneDontChangeIt()
    {
        Log(0, Befallen);
        Log(100, "You have entered an area where levitation effects do not function.");

        Assert.Equal([ReportNone("Befallen")], Log(300, Chat));
    }

    [Fact]
    public void ArenaAreasInsideAZoneDontChangeIt()
    {
        Log(0, Befallen);
        Log(100, "You have entered an Arena (PvP) area.");

        Assert.Equal([ReportNone("Befallen")], Log(300, Chat));
    }

    static ReportBonusAction Report(string zone, string bonus) => new(new BonusReport(Server.Frostreaver, zone, new BonusKey(bonus)));

    static ReportNoBonusAction ReportNone(string zone) => new(Server.Frostreaver, zone);

    IReadOnlyList<ScoutAction> Log(int seconds, string message) => this._scout.Handle(Parser.Parse($"[Thu Oct 29 12:00:00 2026] {message}"), Start.AddSeconds(seconds), Lists);

    IReadOnlyList<ScoutAction> LogWithoutLists(int seconds, string message) => this._scout.Handle(Parser.Parse($"[Thu Oct 29 12:00:00 2026] {message}"), Start.AddSeconds(seconds), null);
}
