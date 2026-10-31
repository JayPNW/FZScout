using FZScout.Logs;
using FZScout.Reporting;
using FZScout.Site;
using FZScout.Tests.Logs;
using FZScout.Tests.Reporting;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

/// <summary>The whole app, from a real log file on disk to requests to the site.</summary>
public class ScoutTests : IDisposable
{
    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    readonly TempFolder _everQuest = new();
    readonly FakeSite _site = new();
    readonly StringWriter _output = new();
    readonly LogWatcher _watcher;
    readonly Scout _scout;
    readonly string _log;
    List<EqProcess> _processes;

    public ScoutTests()
    {
        var logs = Path.Combine(this._everQuest.Path, "Logs");
        Directory.CreateDirectory(logs);
        this._log = Path.Combine(logs, "eqlog_Bob_fangbreaker.txt");
        File.WriteAllText(this._log, string.Empty);
        this._processes = [new EqProcess(100, this._everQuest.File("eqgame.exe"))];

        var log = new ConsoleLog(this._output, new FakeTimeProvider(Start));
        var fangbreakerZoneClient = this._site.Client;
        var serverListCache = new ServerListCache(fangbreakerZoneClient, log);
        var bonusReporter = new BonusReporter(fangbreakerZoneClient, serverListCache, log);
        bonusReporter.SetApiKey(new ApiKey("fz_live_test"));
        this._watcher = new LogWatcher(log);
        this._scout = new Scout(this._watcher, serverListCache, bonusReporter, () => this._processes);
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public void Dispose() => this._everQuest.Dispose();

    [Fact]
    public async Task ReportsABonusFromTheLog()
    {
        await Tick(0);

        Write("You have entered Clan Crushbone.");
        await Tick(1);
        Write("You have slain an orc pawn!");
        Write("This land is saturated with unstable magic, multiplying the loot that may be found here.");
        await Tick(2);

        Assert.Equal(
            [
                "GET /api/servers/fangbreaker/bonuses/types",
                "GET /api/servers/fangbreaker/bonuses/zones",
                """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Crushbone","bonusKey":"loot"}""",
            ],
            this._site.Requests);
    }

    [Fact]
    public async Task ReportsNoneWhenAKillBringsNoBonus()
    {
        await Tick(0);

        Write("You have entered Befallen.");
        Write("You have slain a skeleton!");
        await Tick(1);
        await Tick(5);
        Assert.DoesNotContain(this._site.Requests, r => r.StartsWith("POST", StringComparison.Ordinal));

        await Tick(6);
        Assert.Equal(
            """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Befallen","bonusKey":"none"}""",
            this._site.Requests[^1]);
    }

    [Fact]
    public async Task NoNoneAfterEverQuestExits()
    {
        await Tick(0);
        Write("You have entered Befallen.");
        await Tick(1);

        this._processes = [];
        await Tick(30);
        await Tick(1000);

        Assert.DoesNotContain(this._site.Requests, r => r.StartsWith("POST", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScansForEverQuestEveryThirtySeconds()
    {
        this._processes = [];
        await Tick(0);

        this._processes = [new EqProcess(100, this._everQuest.File("eqgame.exe"))];
        await Tick(29);
        Write("You have entered Befallen.");
        await Tick(29);
        Assert.Empty(this._watcher.ActiveLogs);

        await Tick(30);
        Write("You have entered Befallen.");
        await Tick(31);
        Assert.Equal([this._log], this._watcher.ActiveLogs);
    }

    void Write(string message) => File.AppendAllText(this._log, $"[Thu Oct 29 12:00:00 2026] {message}\r\n");

    Task Tick(int seconds) => this._scout.TickAsync(Start.AddSeconds(seconds), Cancel);
}
