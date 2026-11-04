using System.Net;
using FZScout.Reporting;
using FZScout.Site;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests.Reporting;

public class BonusReporterTests
{
    const string Post = "POST /api/client/bonuses";
    const string Today = "GET /api/servers/fangbreaker/bonuses/today";

    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    readonly FakeSite _site = new();
    readonly StringWriter _output = new();
    readonly BonusReporter _reporter;
    ApiKey? _storedApiKey = new("fz_live_test");

    public BonusReporterTests()
    {
        var log = new Log(this._output, new FakeTimeProvider(Start));
        var fangbreakerZoneClient = this._site.Client;
        this._reporter = new BonusReporter(fangbreakerZoneClient, () => this._storedApiKey, log);
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReportsABonus()
    {
        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
        Assert.Equal("12:00:00 Reported Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    [Theory]
    [InlineData("unconfirmed")]
    [InlineData("experience")]
    [InlineData("none")]
    public async Task ReportsABonusDifferentFromWhatTheSiteShows(string shown)
    {
        this._site.SetToday("Befallen", shown);

        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
    }

    [Fact]
    public async Task DoesNotReportABonusTheSiteAlreadyShows()
    {
        this._site.SetToday("Befallen", "loot");

        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.Equal([Today], this._site.Requests);
        Assert.Equal("12:00:00 https://fangbreaker.zone/ already shows 'loot' for 'Befallen', so it wasn't reported again.", Output());
    }

    [Fact]
    public async Task ReportsABonusWhenTodayCantBeChecked()
    {
        this._site.Fail(Today, HttpStatusCode.InternalServerError);

        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
        Assert.Equal("12:00:00 Reported Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    [Fact]
    public async Task ReportsNoneWhenTheSiteShowsUnconfirmed()
    {
        this._site.SetToday("Befallen", "unconfirmed");

        await this._reporter.ReportNoBonusAsync(Server.Fangbreaker, "Befallen", Cancel);

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "none")], this._site.Requests);
    }

    [Fact]
    public async Task ReportsNoneForAZoneMissingFromToday()
    {
        await this._reporter.ReportNoBonusAsync(Server.Fangbreaker, "Befallen", Cancel);

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "none")], this._site.Requests);
    }

    [Fact]
    public async Task DoesNotReportNoneTheSiteAlreadyShows()
    {
        this._site.SetToday("Befallen", "none");

        await this._reporter.ReportNoBonusAsync(Server.Fangbreaker, "Befallen", Cancel);

        Assert.Equal([Today], this._site.Requests);
        Assert.Equal("12:00:00 https://fangbreaker.zone/ already shows 'none' for 'Befallen', so it wasn't reported again.", Output());
    }

    [Fact]
    public async Task DoesNotReportNoneWhenTheSiteShowsABonus()
    {
        this._site.SetToday("Befallen", "experience");

        await this._reporter.ReportNoBonusAsync(Server.Fangbreaker, "Befallen", Cancel);

        Assert.Equal([Today], this._site.Requests);
        Assert.Equal("12:00:00 https://fangbreaker.zone/ already shows 'experience' for 'Befallen', so 'none' wasn't reported.", Output());
    }

    [Fact]
    public async Task DoesNotReportNoneWhenTodayCantBeChecked()
    {
        this._site.Fail(Today, HttpStatusCode.InternalServerError);

        await this._reporter.ReportNoBonusAsync(Server.Fangbreaker, "Befallen", Cancel);

        Assert.Equal([Today], this._site.Requests);
        Assert.StartsWith("12:00:00 ERROR: Couldn't check today's bonuses, so Fangbreaker 'Befallen' is 'none' wasn't reported.", Output());
    }

    [Fact]
    public async Task BadPayloadIsNotRetried()
    {
        this._site.Fail(Post, HttpStatusCode.BadRequest);

        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
        Assert.Equal(
            """12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'. POST https://fangbreaker.zone/api/client/bonuses returned 400: {"ok":false,"error":"Nope"}""",
            Output());
    }

    [Fact]
    public async Task RejectedKeyIsOnlyLogged()
    {
        this._site.Fail(Post, HttpStatusCode.Unauthorized);

        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.StartsWith("12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot', because the API key was rejected. Enter a new one in Settings.", Output());
    }

    [Fact]
    public async Task DoesNotReportWithoutAnApiKey()
    {
        this._storedApiKey = null;

        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.Equal([Today], this._site.Requests);
        Assert.Equal("12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot', because there's no API key. Enter one in Settings.", Output());
    }

    [Fact]
    public async Task BannedIsOnlyLogged()
    {
        this._site.Fail(Post, HttpStatusCode.Forbidden);
        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");
        await ReportBonus(Server.Fangbreaker, "Crushbone", "loot");

        Assert.Equal(
            [Today, PostBody("Fangbreaker", "Befallen", "loot"), Today, PostBody("Fangbreaker", "Crushbone", "loot")],
            this._site.Requests);
        Assert.StartsWith("12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    [Fact]
    public async Task ZoneNotFoundIsOnlyLogged()
    {
        this._site.Fail(Post, HttpStatusCode.NotFound);

        await ReportBonus(Server.Fangbreaker, "Befallen", "loot");

        Assert.Equal([Today, PostBody("Fangbreaker", "Befallen", "loot")], this._site.Requests);
        Assert.StartsWith("12:00:00 ERROR: Couldn't report Fangbreaker 'Befallen' is 'loot'.", Output());
    }

    Task ReportBonus(Server server, string zone, string bonus)
    {
        return this._reporter.ReportBonusAsync(new BonusReport(server, zone, new BonusKey(bonus)), Cancel);
    }

    static string PostBody(string server, string zone, string bonus)
    {
        return $$"""POST /api/client/bonuses {"serverName":"{{server}}","zoneName":"{{zone}}","bonusKey":"{{bonus}}"}""";
    }

    string Output() => this._output.ToString().TrimEnd();
}
