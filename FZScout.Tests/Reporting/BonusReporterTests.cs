using System.Net;
using FZScout.Reporting;
using FZScout.Scouting;
using FZScout.Site;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests.Reporting;

public class BonusReporterTests
{
    const string Post = "POST /api/client/bonuses";
    const string Today = "GET /api/servers/fangbreaker/bonuses/today";

    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    static readonly Character Bob = new("Bob", Server.Fangbreaker);
    static readonly Character Ann = new("Ann", Server.Frostreaver);

    readonly FakeSite _site = new();
    readonly StringWriter _output = new();
    readonly BonusReporter _reporter;

    public BonusReporterTests()
    {
        var log = new ConsoleReporter(this._output, new FakeTimeProvider(Start));
        var fangbreakerZoneClient = this._site.Client;
        this._reporter = new BonusReporter(fangbreakerZoneClient, new ServerListCache(fangbreakerZoneClient, log), log);
        this._reporter.SetApiKey(new ApiKey("fz_live_test"));
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReportsABonus()
    {
        await Execute(0, Bonus(Bob, "Befallen", "loot"));

        Assert.Equal([PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
        Assert.Equal("12:00:00 Reported Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    [Theory]
    [InlineData("unconfirmed")]
    [InlineData("none")]
    public async Task ReportsNoneWhenTheSiteShowsNoBonus(string shown)
    {
        this._site.SetToday("Befallen", shown);

        await Execute(0, new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "none")], this._site.Requests);
    }

    [Fact]
    public async Task ReportsNoneForAZoneMissingFromToday()
    {
        await Execute(0, new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "none")], this._site.Requests);
    }

    [Fact]
    public async Task DoesNotReportNoneWhenTheSiteShowsABonus()
    {
        this._site.SetToday("Befallen", "experience");

        await Execute(0, new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today], this._site.Requests);
        Assert.Equal("12:00:00 https://fangbreaker.zone/ already shows 'experience' for 'Befallen', so 'none' wasn't reported.", Output());
    }

    [Fact]
    public async Task DoesNotReportNoneWhenTodayCantBeChecked()
    {
        this._site.Fail(Today, HttpStatusCode.InternalServerError);

        await Execute(0, new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today], this._site.Requests);
        Assert.StartsWith("12:00:00 ERROR: Couldn't check today's bonuses, so Fangbreaker 'Befallen' is 'none' wasn't reported.", Output());
    }

    [Fact]
    public async Task LogsErrors()
    {
        await Execute(0, new LogErrorAction("Something broke."));

        Assert.Equal("12:00:00 ERROR: Something broke.", Output());
    }

    [Fact]
    public async Task LogsInfo()
    {
        await Execute(0, new LogInfoAction("Hello."));

        Assert.Equal("12:00:00 Hello.", Output());
    }

    [Fact]
    public async Task WithoutAKeyNothingIsSent()
    {
        this._reporter.SetApiKey(null);

        await Execute(0, Bonus(Bob, "Befallen", "loot"));

        Assert.Empty(this._site.Requests);
        Assert.Equal("12:00:00 WARNING: No API key, so Fangbreaker 'Befallen' is 'loot' wasn't reported.", Output());
    }

    [Fact]
    public async Task BadPayloadIsNotRetried()
    {
        this._site.Fail(Post, HttpStatusCode.BadRequest);

        await Execute(0, Bonus(Bob, "Befallen", "loot"));

        Assert.Single(this._site.Requests);
        Assert.Equal(
            """12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'. POST https://fangbreaker.zone/api/client/bonuses returned 400: {"ok":false,"error":"Nope"}""",
            Output());
    }

    [Fact]
    public async Task RejectedKeyStopsReportingUntilTheKeyChanges()
    {
        this._site.Fail(Post, HttpStatusCode.Unauthorized);
        await Execute(0, Bonus(Bob, "Befallen", "loot"));
        Assert.Contains("ERROR: API key rejected.", Output());

        this._site.Succeed(Post);
        await Execute(10, Bonus(Ann, "Befallen", "loot"));
        Assert.Single(this._site.Requests);

        this._reporter.SetApiKey(new ApiKey("fz_live_new"));
        await Execute(20, Bonus(Ann, "Befallen", "loot"));
        Assert.Equal(2, this._site.Requests.Count);
    }

    [Fact]
    public async Task RejectedKeyIsReportedAtCheck()
    {
        this._site.Fail(Post, HttpStatusCode.Unauthorized);

        await this._reporter.ChangeApiKeyAsync(new ApiKey("fz_live_bad"), Cancel);
        this._site.Succeed(Post);
        await Execute(0, Bonus(Bob, "Befallen", "loot"));

        Assert.False(this._reporter.HasApiKey);
        Assert.Single(this._site.Requests);
        Assert.StartsWith("12:00:00 Checking the API key with fangbreaker.zone.", Output());
        Assert.Contains("""ERROR: POST https://fangbreaker.zone/api/client/bonuses returned 401: {"ok":false,"error":"Nope"}""", Output());
        Assert.Contains("ERROR: API key rejected.", Output());
    }

    [Fact]
    public async Task MissingKeyIsReportedAtCheck()
    {
        await this._reporter.ChangeApiKeyAsync(null, Cancel);

        Assert.False(this._reporter.HasApiKey);
        Assert.Empty(this._site.Requests);
        Assert.Equal("12:00:00 API key missing. Nothing will be reported.", Output());
    }

    [Fact]
    public async Task KeyThatCantBeCheckedIsUsed()
    {
        this._site.Fail(Post, HttpStatusCode.InternalServerError);
        await this._reporter.ChangeApiKeyAsync(new ApiKey("fz_live_new"), Cancel);
        Assert.Contains("12:00:00 WARNING: API key: couldn't check it, so it will be used as is.", Output());

        this._site.Succeed(Post);
        await Execute(0, Bonus(Bob, "Befallen", "loot"));
        Assert.Equal(2, this._site.Requests.Count);
    }

    [Fact]
    public async Task BannedStopsReportingToThatSiteOnly()
    {
        this._site.Fail(Post, HttpStatusCode.Forbidden);
        await Execute(0, Bonus(Bob, "Befallen", "loot"));
        Assert.Contains("ERROR: https://fangbreaker.zone/ has banned or timed out this account. Reporting to it is stopped until restart.", Output());

        this._site.Succeed(Post);
        await Execute(10, Bonus(Bob, "Crushbone", "loot"));
        await Execute(20, Bonus(Ann, "Befallen", "loot"));

        Assert.Equal(
            [PostBody("Fangbreaker", "Befallen", "loot"), PostBody("Frostreaver", "Befallen", "loot")],
            this._site.Requests);
    }

    [Fact]
    public async Task ZoneNotFoundReloadsTheZonesList()
    {
        this._site.Fail(Post, HttpStatusCode.NotFound);

        await Execute(0, Bonus(Bob, "Befallen", "loot"));

        Assert.Equal(
            [PostBody("Fangbreaker", "Befallen", "loot"), "GET /api/servers/fangbreaker/bonuses/types", "GET /api/servers/fangbreaker/bonuses/zones"],
            this._site.Requests);
    }

    static ReportBonusAction Bonus(Character character, string zone, string bonus)
    {
        return new(character, new BonusReport(character.Server, zone, new BonusKey(bonus)));
    }

    static string PostBody(string server, string zone, string bonus)
    {
        return $$"""POST /api/client/bonuses {"serverName":"{{server}}","zoneName":"{{zone}}","bonusKey":"{{bonus}}"}""";
    }

    Task Execute(int seconds, ScoutAction action) => this._reporter.ExecuteAsync(action, Start.AddSeconds(seconds), Cancel);

    string Output() => this._output.ToString().TrimEnd();
}
