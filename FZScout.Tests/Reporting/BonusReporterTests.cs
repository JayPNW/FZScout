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

    readonly FakeSite _site = new();
    readonly StringWriter _output = new();
    readonly BonusReporter _reporter;

    public BonusReporterTests()
    {
        var log = new ConsoleReporter(this._output, new FakeTimeProvider(Start));
        var fangbreakerZoneClient = this._site.Client;
        this._reporter = new BonusReporter(fangbreakerZoneClient, new ApiKey("fz_live_test"), log);
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReportsABonus()
    {
        await Execute(Bonus(Bob, "Befallen", "loot"));

        Assert.Equal([PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
        Assert.Equal("12:00:00 Reported Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    [Theory]
    [InlineData("unconfirmed")]
    [InlineData("none")]
    public async Task ReportsNoneWhenTheSiteShowsNoBonus(string shown)
    {
        this._site.SetToday("Befallen", shown);

        await Execute(new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "none")], this._site.Requests);
    }

    [Fact]
    public async Task ReportsNoneForAZoneMissingFromToday()
    {
        await Execute(new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "none")], this._site.Requests);
    }

    [Fact]
    public async Task DoesNotReportNoneWhenTheSiteShowsABonus()
    {
        this._site.SetToday("Befallen", "experience");

        await Execute(new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today], this._site.Requests);
        Assert.Equal("12:00:00 https://fangbreaker.zone/ already shows 'experience' for 'Befallen', so 'none' wasn't reported.", Output());
    }

    [Fact]
    public async Task DoesNotReportNoneWhenTodayCantBeChecked()
    {
        this._site.Fail(Today, HttpStatusCode.InternalServerError);

        await Execute(new ReportNoBonusAction(Bob, "Befallen"));

        Assert.Equal([Today], this._site.Requests);
        Assert.StartsWith("12:00:00 ERROR: Couldn't check today's bonuses, so Fangbreaker 'Befallen' is 'none' wasn't reported.", Output());
    }

    [Fact]
    public async Task LogsErrors()
    {
        await Execute(new LogErrorAction("Something broke."));

        Assert.Equal("12:00:00 ERROR: Something broke.", Output());
    }

    [Fact]
    public async Task LogsInfo()
    {
        await Execute(new LogInfoAction("Hello."));

        Assert.Equal("12:00:00 Hello.", Output());
    }

    [Fact]
    public async Task BadPayloadIsNotRetried()
    {
        this._site.Fail(Post, HttpStatusCode.BadRequest);

        await Execute(Bonus(Bob, "Befallen", "loot"));

        Assert.Single(this._site.Requests);
        Assert.Equal(
            """12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'. POST https://fangbreaker.zone/api/client/bonuses returned 400: {"ok":false,"error":"Nope"}""",
            Output());
    }

    [Fact]
    public async Task RejectedKeyStopsTheApp()
    {
        this._site.Fail(Post, HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<ApiKeyRejectedException>(() => Execute(Bonus(Bob, "Befallen", "loot")));
        Assert.StartsWith("12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    [Fact]
    public async Task BannedIsOnlyLogged()
    {
        this._site.Fail(Post, HttpStatusCode.Forbidden);
        await Execute(Bonus(Bob, "Befallen", "loot"));
        await Execute(Bonus(Bob, "Crushbone", "loot"));

        Assert.Equal(
            [PostBody("Fangbreaker", "Befallen", "loot"), PostBody("Fangbreaker", "Crushbone", "loot")],
            this._site.Requests);
        Assert.StartsWith("12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    [Fact]
    public async Task ZoneNotFoundIsOnlyLogged()
    {
        this._site.Fail(Post, HttpStatusCode.NotFound);

        await Execute(Bonus(Bob, "Befallen", "loot"));

        Assert.Equal([PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
        Assert.StartsWith("12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    static ReportBonusAction Bonus(Character character, string zone, string bonus)
    {
        return new(character, new BonusReport(character.Server, zone, new BonusKey(bonus)));
    }

    static string PostBody(string server, string zone, string bonus)
    {
        return $$"""POST /api/client/bonuses {"serverName":"{{server}}","zoneName":"{{zone}}","bonusKey":"{{bonus}}"}""";
    }

    Task Execute(ScoutAction action) => this._reporter.ExecuteAsync(action, Cancel);

    string Output() => this._output.ToString().TrimEnd();
}
