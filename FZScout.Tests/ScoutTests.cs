using FZScout.Reporting;
using FZScout.Scouting;
using FZScout.Site;
using FZScout.Tests.Reporting;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

/// <summary>The whole app's scouting pipeline, from an EverQuestLogWatcher event to a request to the site.</summary>
public class ScoutTests
{
    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    static readonly EverQuestLogWatcher.Character Bob = new("Bob", "fangbreaker");

    readonly FakeSite _site = new();
    readonly StringWriter _output = new();
    readonly Scout _scout;

    public ScoutTests()
    {
        var log = new Log(this._output, new FakeTimeProvider(Start));
        var fangbreakerZoneClient = this._site.Client;
        var serverListCache = new ServerListCache(fangbreakerZoneClient, log);
        var bonusReporter = new BonusReporter(fangbreakerZoneClient, () => new ApiKey("fz_live_test"), log);
        this._scout = new Scout(serverListCache, log, bonusReporter);
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReportsABonusFromTheLog()
    {
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Clan Crushbone"));
        await Handle(1, new EverQuestLogWatcher.MobSlainByYou(Bob, Start.UtcDateTime, ""));
        await Handle(1, new EverQuestLogWatcher.AreaIsSaturatedWithUnstableMagic(Bob, Start.UtcDateTime, "", "multiplying the loot that may be found here"));

        Assert.Equal(
            [
                "GET /api/servers/fangbreaker/bonuses/types",
                "GET /api/servers/fangbreaker/bonuses/zones",
                "GET /api/servers/fangbreaker/bonuses/today",
                """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Crushbone","bonusKey":"loot"}""",
            ],
            this._site.Requests);
    }

    [Fact]
    public async Task ReportsNoneWhenAKillBringsNoBonus()
    {
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Befallen"));
        await Handle(1, new EverQuestLogWatcher.MobSlainByYou(Bob, Start.UtcDateTime, ""));

        var nextMessageWellPastTheKillTimeout = new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Befallen");
        await Handle(6, nextMessageWellPastTheKillTimeout);

        Assert.Equal(
            """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Befallen","bonusKey":"none"}""",
            this._site.Requests[^1]);
    }

    [Fact]
    public async Task ServerErrorIsNotRetried()
    {
        const string Report = """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Crushbone","bonusKey":"loot"}""";
        this._site.Fail("POST /api/client/bonuses", System.Net.HttpStatusCode.InternalServerError);
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Clan Crushbone"));
        await Handle(1, new EverQuestLogWatcher.AreaIsSaturatedWithUnstableMagic(Bob, Start.UtcDateTime, "", "multiplying the loot that may be found here"));
        this._site.Succeed("POST /api/client/bonuses");

        await Handle(600, new EverQuestLogWatcher.SayChatMessageFromYou(Bob, Start.UtcDateTime, ""));

        Assert.Single(this._site.Requests, Report);
    }

    [Fact]
    public async Task LogsForOtherServersAreIgnored()
    {
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(new("Bob", "teek"), Start.UtcDateTime, "", "Befallen"));
        await Handle(1, new EverQuestLogWatcher.MobSlainByYou(new("Bob", "teek"), Start.UtcDateTime, ""));

        Assert.Empty(this._site.Requests);
    }

    [Fact]
    public async Task UninterestingEventsAreIgnored()
    {
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Befallen"));
        await Handle(1, new EverQuestLogWatcher.SayChatMessageFromYou(Bob, Start.UtcDateTime, ""));

        Assert.DoesNotContain(this._site.Requests, r => r.StartsWith("POST", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WaitsForABonusMessageAfterEnteringAZone()
    {
        var changes = RecordWaitingForBonusMessageUntilChanges();

        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Befallen"));

        Assert.Equal([Start + CharacterScout.IdleTimeout], changes);
    }

    [Fact]
    public async Task StopsWaitingWhenTheBonusIsReported()
    {
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Clan Crushbone"));
        var changes = RecordWaitingForBonusMessageUntilChanges();

        await Handle(1, new EverQuestLogWatcher.AreaIsSaturatedWithUnstableMagic(Bob, Start.UtcDateTime, "", "multiplying the loot that may be found here"));

        Assert.Equal([null], changes);
    }

    [Fact]
    public async Task AKillShortensTheWait()
    {
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Befallen"));
        var changes = RecordWaitingForBonusMessageUntilChanges();

        await Handle(1, new EverQuestLogWatcher.MobSlainByYou(Bob, Start.UtcDateTime, ""));

        Assert.Equal([Start.AddSeconds(1) + CharacterScout.KillTimeout], changes);
    }

    [Fact]
    public async Task WaitsUntilTheLastCharacterStopsWaiting()
    {
        var alice = new EverQuestLogWatcher.Character("Alice", "fangbreaker");
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Befallen"));
        var changes = RecordWaitingForBonusMessageUntilChanges();

        await Handle(60, new EverQuestLogWatcher.ZoneEntered(alice, Start.UtcDateTime, "", "Befallen"));
        await Handle(61, new EverQuestLogWatcher.MobSlainByYou(Bob, Start.UtcDateTime, ""));

        Assert.Equal([Start.AddSeconds(60) + CharacterScout.IdleTimeout], changes);
    }

    [Fact]
    public async Task UninterestingEventsDoNotChangeTheWait()
    {
        await Handle(0, new EverQuestLogWatcher.ZoneEntered(Bob, Start.UtcDateTime, "", "Befallen"));
        var changes = RecordWaitingForBonusMessageUntilChanges();

        await Handle(1, new EverQuestLogWatcher.SayChatMessageFromYou(Bob, Start.UtcDateTime, ""));

        Assert.Empty(changes);
    }

    List<DateTimeOffset?> RecordWaitingForBonusMessageUntilChanges()
    {
        var changes = new List<DateTimeOffset?>();
        this._scout.WaitingForBonusMessageUntilChanged += changes.Add;
        return changes;
    }

    Task Handle(int seconds, EverQuestLogWatcher.EverQuestLogEvent logEvent) => this._scout.HandleAsync(logEvent, Start.AddSeconds(seconds), Cancel);
}
