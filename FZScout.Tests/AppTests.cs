using FZScout.Site;
using FZScout.Tests.Reporting;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

/// <summary>The whole app as launched, with in-memory versions of everything outside it.</summary>
public class AppTests
{
    static readonly ApiKey StoredApiKey = new("fz_live_stored");

    const string KeyCheckRequest = """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"The Nexus","bonusKey":"none"}""";

    readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    readonly StringWriter _output = new();
    readonly FakeSite _site = new();
    readonly InMemoryUserInput _userInput = new(Cancel);
    readonly InMemorySingleInstance _singleInstance = new();
    readonly InMemoryApiKeyStore _apiKeyStore = new();
    readonly App _app;

    public AppTests()
    {
        this._app = new App(new AppDependencies(
            this._userInput,
            new Log(this._output, this._time),
            GetVersion: () => "1",
            this._time,
            TryClaimSingleInstance: this._singleInstance.TryClaim,
            this._apiKeyStore,
            this._site.Client));
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    string Output => this._output.ToString();

    [Fact]
    public async Task InformsUserAndExitsWhenAlreadyRunning()
    {
        this._apiKeyStore.Save(StoredApiKey);
        using var otherInstance = this._singleInstance.TryClaim();

        await this._app.RunAsync();

        Assert.Equal(1, this._userInput.TimesInformedAppIsAlreadyRunning);
        Assert.Empty(this._site.Requests);
    }

    [Fact]
    public async Task ChecksTheStoredApiKeyAtStartup()
    {
        this._apiKeyStore.Save(StoredApiKey);

        await this._app.LogProblemWithStoredApiKeyAsync(Cancel);

        Assert.Equal([KeyCheckRequest], this._site.Requests);
        Assert.DoesNotContain("ERROR", Output);
    }

    [Fact]
    public async Task LogsWhyTheSiteRejectedTheStoredApiKey()
    {
        this._apiKeyStore.Save(StoredApiKey);
        this._site.RejectApiKey(StoredApiKey);

        await this._app.LogProblemWithStoredApiKeyAsync(Cancel);

        Assert.Contains("""ERROR: API key rejected. Enter a new one in Settings. POST https://fangbreaker.zone/api/client/bonuses returned 401: {"ok":false,"error":"Nope"}""", Output);
        Assert.Equal(StoredApiKey, this._apiKeyStore.Load());
    }

    [Fact]
    public async Task LogsThatThereIsNoApiKey()
    {
        await this._app.LogProblemWithStoredApiKeyAsync(Cancel);

        Assert.Empty(this._site.Requests);
        Assert.Contains("ERROR: No API key. Enter one in Settings.", Output);
    }

    [Fact]
    public async Task SaysNothingWhenTheStoredApiKeyCantBeChecked()
    {
        this._apiKeyStore.Save(StoredApiKey);
        this._site.Fail("POST /api/client/bonuses", System.Net.HttpStatusCode.InternalServerError);

        await this._app.LogProblemWithStoredApiKeyAsync(Cancel);

        Assert.DoesNotContain("ERROR", Output);
    }
}
