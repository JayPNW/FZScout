using System.Net;
using FZScout.Site;

namespace FZScout.Tests.Site;

public class FangbreakerZoneClientTests
{
    static readonly ApiKey Key = new("fz_live_test");

    [Fact]
    public async Task GetTypes()
    {
        var (fangbreakerZoneClient, handler) = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[
              {"id":"87b99f7c-af3e-4b01-96ed-b8ec015612f1","key":"experience","label":"Experience","sortOrder":10},
              {"id":"9c7c3f71-8094-44d9-b041-7df50b4e001e","key":"none","label":"No Bonus","sortOrder":999}]}
            """);

        var result = await fangbreakerZoneClient.GetBonusTypesAsync(Server.Frostreaver, TestContext.Current.CancellationToken);

        Assert.Equal(ApiOutcome.Success, result.Outcome);
        Assert.Equal(["experience", "none"], result.Value!.Select(k => k.Value).Order());
        Assert.Equal(new Uri("https://frostreaver.zone/api/servers/frostreaver/bonuses/types"), handler.Sent.Single().Uri);
        Assert.Equal(HttpMethod.Get, handler.Sent.Single().Method);
    }

    [Fact]
    public async Task GetZones()
    {
        var (fangbreakerZoneClient, handler) = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[
              {"id":"8de71326-2b5a-4db2-b5fa-348ff0ab24a8","slug":"akanon","name":"Ak'Anon","expansion":"Classic","minLevel":1,"maxLevel":15,"zoneType":"Indoor","isActive":true},
              {"id":"c9b0852f-cacd-48f8-8cab-affb47b5b4c0","slug":"befallen","name":"Befallen","expansion":"Classic","minLevel":5,"maxLevel":20,"zoneType":"Indoor","isActive":true}]}
            """);

        var result = await fangbreakerZoneClient.GetZonesAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(["Ak'Anon", "Befallen"], result.Value!.Order());
        Assert.Equal(new Uri("https://fangbreaker.zone/api/servers/fangbreaker/bonuses/zones"), handler.Sent.Single().Uri);
    }

    [Fact]
    public async Task GetToday()
    {
        var (fangbreakerZoneClient, handler) = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[
              {"id":null,"zoneId":"38bd03f2-006f-49c1-ab10-267831ec9760","zoneSlug":"abysmal-sea","name":"Abysmal Sea","bonusTypeId":null,"bonus":"unconfirmed","bonusLabel":"Unconfirmed","state":"UNCONFIRMED","bonusDate":"2026-10-29","pendingRemovalBy":null},
              {"id":"f53fd95b-05e9-4e9f-968f-4e30c91d1020","zoneId":"d370ffac-ffa1-43ed-94c3-72df553483cf","zoneSlug":"bastion-of-thunder","name":"Bastion of Thunder","bonusTypeId":"87b99f7c-af3e-4b01-96ed-b8ec015612f1","bonus":"experience","bonusLabel":"Experience","state":"ACTIVE","bonusDate":"2026-10-29","pendingRemovalBy":null}]}
            """);

        var result = await fangbreakerZoneClient.GetTodayAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(BonusKey.Unconfirmed, result.Value!["Abysmal Sea"]);
        Assert.Equal(new BonusKey("experience"), result.Value["Bastion of Thunder"]);
        Assert.Equal(new Uri("https://fangbreaker.zone/api/servers/fangbreaker/bonuses/today"), handler.Sent.Single().Uri);
    }

    [Fact]
    public async Task GetTodayToleratesADuplicateZone()
    {
        var (fangbreakerZoneClient, _) = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[{"name":"Befallen","bonus":"loot"},{"name":"Befallen","bonus":"loot"}]}
            """);

        var result = await fangbreakerZoneClient.GetTodayAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(new BonusKey("loot"), result.Value!["Befallen"]);
    }

    [Fact]
    public async Task ReportBonus()
    {
        var (fangbreakerZoneClient, handler) = Create(HttpStatusCode.OK, """
            {"ok":true,"data":{"updated":false,"zoneBonus":{"id":"f53fd95b-05e9-4e9f-968f-4e30c91d1020","serverId":1,"state":"ACTIVE"}}}
            """);

        var report = new BonusReport(Server.Frostreaver, "Bastion of Thunder", new BonusKey("experience"));
        var result = await fangbreakerZoneClient.ReportBonusAsync(report, Key, TestContext.Current.CancellationToken);

        Assert.Equal(ApiOutcome.Success, result.Outcome);
        Assert.False(result.Value);
        var sent = handler.Sent.Single();
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal(new Uri("https://frostreaver.zone/api/client/bonuses"), sent.Uri);
        Assert.Equal("fz_live_test", sent.Headers["X-API-Key"]);
        Assert.Equal("""{"serverName":"Frostreaver","zoneName":"Bastion of Thunder","bonusKey":"experience"}""", sent.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ApiOutcome.BadPayload)]
    [InlineData(HttpStatusCode.Unauthorized, ApiOutcome.KeyRejected)]
    [InlineData(HttpStatusCode.Forbidden, ApiOutcome.Banned)]
    [InlineData(HttpStatusCode.NotFound, ApiOutcome.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError, ApiOutcome.Unavailable)]
    [InlineData(HttpStatusCode.BadGateway, ApiOutcome.Unavailable)]
    public async Task FailedStatus(HttpStatusCode httpStatusCode, ApiOutcome expected)
    {
        var (fangbreakerZoneClient, _) = Create(httpStatusCode, """{"ok":false,"error":"Server not found"}""");

        var report = new BonusReport(Server.Fangbreaker, "Befallen", BonusKey.None);
        var result = await fangbreakerZoneClient.ReportBonusAsync(report, Key, TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Outcome);
        Assert.Equal(
            $$"""POST https://fangbreaker.zone/api/client/bonuses returned {{(int)httpStatusCode}}: {"ok":false,"error":"Server not found"}""",
            result.Error);
    }

    [Fact]
    public async Task NetworkFailureIsUnavailable()
    {
        var handler = new StubHttpHandler(new HttpRequestException("No such host is known."));
        var fangbreakerZoneClient = new FangbreakerZoneClient(new HttpClient(handler));

        var result = await fangbreakerZoneClient.GetBonusTypesAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(ApiOutcome.Unavailable, result.Outcome);
        Assert.Equal("GET https://fangbreaker.zone/api/servers/fangbreaker/bonuses/types failed: No such host is known.", result.Error);
    }

    [Theory]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("""{"ok":false}""")]
    public async Task UnreadableBodyIsUnavailable(string body)
    {
        var (fangbreakerZoneClient, _) = Create(HttpStatusCode.OK, body);

        var result = await fangbreakerZoneClient.GetBonusTypesAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(ApiOutcome.Unavailable, result.Outcome);
    }

    [Fact]
    public void ApiKeyIsHiddenWhenFormatted()
    {
        Assert.Equal("key fz_live_****", $"key {Key}");
    }

    static (FangbreakerZoneClient Client, StubHttpHandler Handler) Create(HttpStatusCode httpStatusCode, string body)
    {
        var handler = new StubHttpHandler(httpStatusCode, body);
        return (new FangbreakerZoneClient(new HttpClient(handler)), handler);
    }
}
