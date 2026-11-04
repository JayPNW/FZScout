using FZScout.Site;
using FZScout.Tests.Reporting;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

/// <summary>The whole app as launched, with in-memory versions of everything outside it.</summary>
public class AppTests
{
    static readonly ApiKey StoredApiKey = new("fz_live_stored");
    static readonly ApiKey TypedApiKey = new("fz_live_typed");

    const string KeyCheckRequest = """POST /api/client/bonuses {"serverName":"Fangbreaker","zoneName":"The Nexus","bonusKey":"none"}""";

    readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    readonly StringWriter _output = new();
    readonly FakeSite _site = new();
    readonly InMemoryUserInput _userInput = new(Cancel, TypedApiKey);
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

        await this._app.GetAcceptedApiKeyAsync();

        Assert.Equal([KeyCheckRequest], this._site.Requests);
        Assert.Contains("API key verified.", Output);
        Assert.Equal(0, this._userInput.TimesAskedForApiKey);
    }

    [Fact]
    public async Task AsksForAnApiKeyWhenNoneIsStored()
    {
        await this._app.GetAcceptedApiKeyAsync();

        Assert.Equal(1, this._userInput.TimesAskedForApiKey);
        Assert.Equal(TypedApiKey, this._apiKeyStore.Load());
        Assert.Equal([KeyCheckRequest], this._site.Requests);
        Assert.Contains("API key verified.", Output);
    }

    [Fact]
    public async Task AsksForANewApiKeyWhenTheSiteRejectsTheStoredOne()
    {
        this._apiKeyStore.Save(StoredApiKey);
        this._site.RejectApiKey(StoredApiKey);

        await this._app.GetAcceptedApiKeyAsync();

        Assert.Contains("ERROR: API key rejected.", Output);
        Assert.Equal(1, this._userInput.TimesAskedForApiKey);
        Assert.Equal(TypedApiKey, this._apiKeyStore.Load());
        Assert.Contains("API key verified.", Output);
    }

    [Fact]
    public async Task ShowsWhyTheSiteRejectedTheKey()
    {
        this._apiKeyStore.Save(StoredApiKey);
        this._site.RejectApiKey(StoredApiKey);

        await this._app.GetAcceptedApiKeyAsync();

        Assert.Contains("""ERROR: POST https://fangbreaker.zone/api/client/bonuses returned 401: {"ok":false,"error":"Nope"}""", Output);
    }

    [Fact]
    public async Task UsesAStoredKeyThatCantBeChecked()
    {
        this._apiKeyStore.Save(StoredApiKey);
        this._site.Fail("POST /api/client/bonuses", System.Net.HttpStatusCode.InternalServerError);

        Assert.Equal(StoredApiKey, await this._app.GetAcceptedApiKeyAsync());
        Assert.Contains("WARNING: API key: couldn't check it, so it will be used as is.", Output);
        Assert.Equal(0, this._userInput.TimesAskedForApiKey);
    }

    [Fact]
    public async Task ExitsWhenNoApiKeyIsEntered()
    {
        this._userInput.TypedApiKey = null;

        await this._app.RunAsync();

        Assert.Equal(1, this._userInput.TimesAskedForApiKey);
        Assert.Empty(this._site.Requests);
        Assert.Contains("No API key entered. Exiting.", Output);
    }
}
