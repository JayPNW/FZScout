using System.Net;
using FZScout.Logs;
using FZScout.Site;
using FZScout.Tests.Logs;
using FZScout.Tests.Reporting;
using FZScout.Tests.Site;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

/// <summary>The whole app as launched, with in-memory versions of everything outside it.</summary>
public class AppTests : IDisposable
{
    static readonly ApiKey StoredApiKey = new("fz_live_stored");
    static readonly ApiKey TypedApiKey = new("fz_live_typed");

    const string KeyCheckRequest = """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"The Nexus","bonusKey":"none"}""";

    readonly TempFolder _everQuest = new();
    readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    readonly StringWriter _output = new();
    readonly FakeSite _site = new();
    readonly InMemorySingleInstance _singleInstance = new();
    readonly InMemoryApiKeyStore _apiKeyStore = new();
    readonly string _log;
    readonly App _app;
    int _timesAskedForApiKey;
    int _timesWaitedForAnyKey;

    public AppTests()
    {
        var logs = Path.Combine(this._everQuest.Path, "Logs");
        Directory.CreateDirectory(logs);
        this._log = Path.Combine(logs, "eqlog_Bob_fangbreaker.txt");
        File.WriteAllText(this._log, string.Empty);
        IReadOnlyCollection<EqProcess> processes = [new EqProcess(100, this._everQuest.File("eqgame.exe"))];

        this._app = new App(new AppDependencies(
            ExitRequested: Cancel,
            GetVersion: () => "1",
            this._time,
            new ConsoleReporter(this._output, this._time),
            TryClaimSingleInstance: this._singleInstance.TryClaim,
            WaitForAnyKey: () => this._timesWaitedForAnyKey++,
            ReadApiKeyAsync: TypeApiKeyAsync,
            this._apiKeyStore,
            this._site.Client,
            new GitHubUpdateChecker(new StubHttpHandler(HttpStatusCode.NotFound, string.Empty)),
            FindEqProcesses: () => processes));
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    string Output => this._output.ToString();

    public void Dispose() => this._everQuest.Dispose();

    [Fact]
    public async Task WaitsForAKeyAndExitsWhenAlreadyRunning()
    {
        this._apiKeyStore.Save(StoredApiKey);
        using var otherInstance = this._singleInstance.TryClaim();

        await this._app.RunAsync();

        Assert.Contains("ERROR: FZScout is already running.", Output);
        Assert.Equal(1, this._timesWaitedForAnyKey);
        Assert.Empty(this._site.Requests);
    }

    [Fact]
    public async Task ChecksTheStoredApiKeyAtStartup()
    {
        this._apiKeyStore.Save(StoredApiKey);

        await this._app.StartAsync();

        Assert.Equal([KeyCheckRequest], this._site.Requests);
        Assert.Contains("API key verified.", Output);
        Assert.Equal(0, this._timesAskedForApiKey);
    }

    [Fact]
    public async Task AsksForAnApiKeyWhenNoneIsStored()
    {
        await this._app.StartAsync();
        Assert.Empty(this._site.Requests);

        await Tick();

        Assert.Equal(1, this._timesAskedForApiKey);
        Assert.Equal(TypedApiKey, this._apiKeyStore.Load());
        Assert.Equal([KeyCheckRequest], this._site.Requests);
        Assert.Contains("API key verified.", Output);
    }

    [Fact]
    public async Task AsksForANewApiKeyWhenTheSiteRejectsTheStoredOne()
    {
        this._apiKeyStore.Save(StoredApiKey);
        this._site.Fail("POST /api/client/bonuses", HttpStatusCode.Unauthorized);
        await this._app.StartAsync();
        Assert.Contains("API key: rejected.", Output);

        this._site.Succeed("POST /api/client/bonuses");
        await Tick();

        Assert.Equal(1, this._timesAskedForApiKey);
        Assert.Equal(TypedApiKey, this._apiKeyStore.Load());
        Assert.Contains("API key verified.", Output);
    }

    [Fact]
    public async Task ReportsABonusFromTheLog()
    {
        this._apiKeyStore.Save(StoredApiKey);
        await this._app.StartAsync();
        Assert.Contains($"EverQuest logs found in {Path.GetDirectoryName(this._log)}", Output);

        Write("You have entered Clan Crushbone.");
        await Tick();
        Write("You have slain an orc pawn!");
        Write("This land is saturated with unstable magic, multiplying the loot that may be found here.");
        await Tick();

        Assert.Equal(
            """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"Crushbone","bonusKey":"loot"}""",
            this._site.Requests[^1]);
    }

    Task<string> TypeApiKeyAsync(CancellationToken _)
    {
        this._timesAskedForApiKey++;
        return Task.FromResult(TypedApiKey.Value);
    }

    void Write(string message) => File.AppendAllText(this._log, $"[Thu Oct 29 12:00:00 2026] {message}\r\n");

    Task Tick()
    {
        this._time.Advance(App.TickInterval);
        return this._app.TickAsync();
    }
}
