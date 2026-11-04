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

    readonly List<ScoutAction> _executedActions = [];
    readonly CharacterScout _scout;

    public CharacterScoutTests()
    {
        this._scout = new(Bob, RecordExecutedAction);
    }

    [Fact]
    public async Task BonusMessageIsReported()
    {
        await Log(0, Befallen);

        Assert.Equal([Report("Befallen", "experience")], await Log(10, Experience));
    }

    [Fact]
    public async Task ZoneIsReportedByItsSiteName()
    {
        await Log(0, Crushbone);

        Assert.Equal([Report("Crushbone", "experience")], await Log(10, Experience));
    }

    [Fact]
    public async Task NoBonusFiveMinutesAfterEnteringIsReportedAsNone()
    {
        await Log(0, Befallen);

        Assert.Empty(await Log(299, Chat));
        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }

    [Fact]
    public async Task NoneIsReportedOnlyOnce()
    {
        await Log(0, Befallen);
        await Log(300, Chat);

        Assert.Empty(await Log(600, Chat));
    }

    [Fact]
    public async Task NoBonusFiveSecondsAfterAKillIsReportedAsNone()
    {
        await Log(0, Befallen);
        await Log(20, Kill);

        Assert.Empty(await Log(24, Chat));
        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
        Assert.Empty(await Log(300, Chat));
    }

    [Fact]
    public async Task KillByOwnPetCountsAsAKill()
    {
        await Log(0, Befallen);
        await Log(20, "A wolf was slain by Bob`s pet!");

        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
    }

    [Fact]
    public async Task KillByOwnWarderCountsAsAKill()
    {
        await Log(0, Befallen);
        await Log(20, "A gnoll pup was slain by Bob`s warder!");

        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
    }

    [Fact]
    public async Task KillByAnotherPlayersPetIsNotAKill()
    {
        await Log(0, Befallen);
        await Log(20, "A wolf was slain by Alice`s pet!");

        Assert.Empty(await Log(25, Chat));
    }

    [Fact]
    public async Task LaterKillsDoNotPushTheKillTimerBack()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(23, Kill);

        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
    }

    [Fact]
    public async Task KillsAfterABonusDoNothing()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(20, Experience);
        await Log(40, Kill);

        Assert.Empty(await Log(300, Chat));
    }

    [Fact]
    public async Task BonusAfterReportingNoneIsStillReported()
    {
        // A kill doesn't always trigger the message; sometimes it comes minutes later.
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(25, Chat);

        Assert.Equal([Report("Befallen", "experience")], await Log(200, Experience));
    }

    [Fact]
    public async Task EnteringAnotherZoneRestartsTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(100, Crushbone);

        Assert.Empty(await Log(300, Chat));
        Assert.Equal([ReportNone("Crushbone")], await Log(400, Chat));
    }

    [Fact]
    public async Task EnteringAnotherZoneCancelsTheKillTimer()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(22, Crushbone);

        Assert.Empty(await Log(30, Chat));
    }

    [Fact]
    public async Task ReEnteringTheSameZoneStartsANewVisit()
    {
        await Log(0, Befallen);
        await Log(10, Experience);
        await Log(100, Befallen);

        Assert.Equal([ReportNone("Befallen")], await Log(400, Chat));
    }

    [Fact]
    public async Task DailyResetRestartsTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(10, Experience);
        await Log(1000, DailyReset);

        Assert.Empty(await Log(1299, Chat));
        Assert.Equal([ReportNone("Befallen")], await Log(1300, Chat));
    }

    [Fact]
    public async Task DailyResetRearmsKills()
    {
        await Log(0, Befallen);
        await Log(10, Experience);
        await Log(1000, DailyReset);
        await Log(1010, Kill);

        Assert.Equal([ReportNone("Befallen")], await Log(1015, Chat));
    }

    [Fact]
    public async Task CampingStopsTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(200, Camping);
        await Log(205, "It will take about 25 more seconds to prepare your camp.");

        Assert.Empty(await Log(10_000, Chat));
    }

    [Fact]
    public async Task AbandoningCampDoesNotRestartTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(200, Camping);
        await Log(210, CampAbandoned);

        Assert.Empty(await Log(10_000, Chat));
    }

    [Fact]
    public async Task EnteringAZoneIsLogged()
    {
        Assert.Equal([new LogInfoAction($"Bob (Frostreaver) entered The Greater Faydark, waiting until {Start.AddMinutes(5).ToLocalTime():HH:mm:ss} for a bonus message.")], await Log(0, "You have entered The Greater Faydark."));
    }

    [Fact]
    public async Task BonusBeforeAnyZoneIsIgnored()
    {
        Assert.Empty(await Log(0, Experience));
        Assert.Empty(await Log(1000, Chat));
    }

    [Fact]
    public async Task BonusTypeNotOnTheServersListIsAnError()
    {
        await Log(0, Befallen);

        Assert.Equal(
            [new LogErrorAction($"Bob (Frostreaver) saw a 'tribute' bonus in 'Befallen', which isn't on the Frostreaver bonus types list. Please report it at {CharacterScout.ReportIssuesUrl}")],
            await Log(10, Tribute));
    }

    [Fact]
    public async Task BonusTypeNotOnTheServersListMeansNoneIsntReported()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(21, Tribute);

        Assert.Empty(await Log(25, Chat));
        Assert.Empty(await Log(300, Chat));
    }

    [Fact]
    public async Task UnknownZoneWithNoBonusIsNotAnError()
    {
        Assert.Equal(
            [new LogInfoAction("Bob (Frostreaver) entered The Plane of Knowledge.")],
            await Log(0, "You have entered The Plane of Knowledge."));
        Assert.Empty(await Log(10, Kill));
        Assert.Empty(await Log(1000, Chat));
    }

    [Fact]
    public async Task BonusInAnUnknownZoneIsAnError()
    {
        await Log(0, "You have entered The Halls of Honor.");

        Assert.Equal(
            [new LogErrorAction($"Bob (Frostreaver) saw a 'experience' bonus in 'The Halls of Honor', which isn't on the Frostreaver zones list. Please report it at {CharacterScout.ReportIssuesUrl}")],
            await Log(10, Experience));
    }

    [Fact]
    public async Task ZoneEnteredWithoutListsIsResolvedWhenTheyLoad()
    {
        Assert.Equal([new LogInfoAction($"Bob (Frostreaver) entered Befallen, waiting until {Start.AddMinutes(5).ToLocalTime():HH:mm:ss} for a bonus message.")], await LogWithoutLists(0, Befallen));
        Assert.Empty(await LogWithoutLists(60, Chat));

        Assert.Empty(await Log(61, Chat));
        Assert.Equal(new KnownZone("Befallen"), this._scout.CurrentZone);
        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }

    [Fact]
    public async Task BonusAfterTheListsLoadIsReported()
    {
        await LogWithoutLists(0, Befallen);

        Assert.Equal([Report("Befallen", "experience")], await Log(10, Experience));
    }

    [Fact]
    public async Task ZoneEnteredWithoutListsThatTurnsOutUnknownStopsWaiting()
    {
        await LogWithoutLists(0, "You have entered The Halls of Honor.");

        Assert.Empty(await Log(60, Chat));
        Assert.Equal(new UnknownZone("The Halls of Honor"), this._scout.CurrentZone);
        Assert.Empty(await Log(1000, Chat));
    }

    [Fact]
    public async Task ListsThatNeverLoadMeanNoneIsntReported()
    {
        await LogWithoutLists(0, Befallen);

        Assert.Equal(
            [new LogErrorAction("Bob (Frostreaver) was in 'Befallen', but the Frostreaver zones list still isn't loaded, so 'none' wasn't reported.")],
            await LogWithoutLists(300, Chat));
        Assert.Empty(await LogWithoutLists(1000, Chat));
    }

    [Fact]
    public async Task BonusWhileListsAreMissingIsAnError()
    {
        await LogWithoutLists(0, Befallen);

        Assert.Equal(
            [new LogErrorAction("Bob (Frostreaver) saw a 'experience' bonus in 'Befallen', but the Frostreaver zones list isn't loaded, so it wasn't reported.")],
            await LogWithoutLists(10, Experience));
    }

    [Fact]
    public async Task AreasInsideAZoneDontChangeIt()
    {
        await Log(0, Befallen);
        await Log(100, "You have entered an area where levitation effects do not function.");

        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }

    [Fact]
    public async Task ArenaAreasInsideAZoneDontChangeIt()
    {
        await Log(0, Befallen);
        await Log(100, "You have entered an Arena (PvP) area.");

        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }

    static ReportBonusAction Report(string zone, string bonus) => new(new BonusReport(Server.Frostreaver, zone, new BonusKey(bonus)));

    static ReportNoBonusAction ReportNone(string zone) => new(Server.Frostreaver, zone);

    Task RecordExecutedAction(ScoutAction action, CancellationToken cancellationToken)
    {
        this._executedActions.Add(action);
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<ScoutAction>> Log(int seconds, string message) => Handle(seconds, message, Lists);

    Task<IReadOnlyList<ScoutAction>> LogWithoutLists(int seconds, string message) => Handle(seconds, message, null);

    async Task<IReadOnlyList<ScoutAction>> Handle(int seconds, string message, ServerLists? lists)
    {
        this._executedActions.Clear();
        await this._scout.HandleAsync(Parser.Parse($"[Thu Oct 29 12:00:00 2026] {message}"), Start.AddSeconds(seconds), lists, TestContext.Current.CancellationToken);
        return [.. this._executedActions];
    }
}
