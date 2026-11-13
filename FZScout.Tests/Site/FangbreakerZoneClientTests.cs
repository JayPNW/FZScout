using System.Net;
using FZScout.Site;

namespace FZScout.Tests.Site;

public class FangbreakerZoneClientTests
{
    static readonly ApiKey Key = new("fz_live_test");

    [Fact]
    public async Task GetTypes()
    {
        var fangbreakerZoneClient = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[
              {"id":"87b99f7c-af3e-4b01-96ed-b8ec015612f1","key":"experience","label":"Experience","sortOrder":10},
              {"id":"9c7c3f71-8094-44d9-b041-7df50b4e001e","key":"none","label":"No Bonus","sortOrder":999}]}
            """);

        var result = await fangbreakerZoneClient.GetBonusTypesAsync(Server.Frostreaver, TestContext.Current.CancellationToken);

        Assert.Equal(ApiOutcome.Success, result.Outcome);
        Assert.Equal(["experience", "none"], result.Value!.Select(k => k.Value).Order());
    }

    [Fact]
    public async Task GetZones()
    {
        var fangbreakerZoneClient = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[
              {"id":"8de71326-2b5a-4db2-b5fa-348ff0ab24a8","slug":"akanon","name":"Ak'Anon","expansion":"Classic","minLevel":1,"maxLevel":15,"zoneType":"Indoor","isActive":true},
              {"id":"c9b0852f-cacd-48f8-8cab-affb47b5b4c0","slug":"befallen","name":"Befallen","expansion":"Classic","minLevel":5,"maxLevel":20,"zoneType":"Indoor","isActive":true}]}
            """);

        var result = await fangbreakerZoneClient.GetZonesAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(["Ak'Anon", "Befallen"], result.Value!.Order());
    }

    [Fact]
    public async Task GetToday()
    {
        var fangbreakerZoneClient = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[
              {"id":null,"zoneId":"38bd03f2-006f-49c1-ab10-267831ec9760","zoneSlug":"abysmal-sea","name":"Abysmal Sea","bonusTypeId":null,"bonus":"unconfirmed","bonusLabel":"Unconfirmed","state":"UNCONFIRMED","bonusDate":"2026-10-29","pendingRemovalBy":null},
              {"id":"f53fd95b-05e9-4e9f-968f-4e30c91d1020","zoneId":"d370ffac-ffa1-43ed-94c3-72df553483cf","zoneSlug":"bastion-of-thunder","name":"Bastion of Thunder","bonusTypeId":"87b99f7c-af3e-4b01-96ed-b8ec015612f1","bonus":"experience","bonusLabel":"Experience","state":"ACTIVE","bonusDate":"2026-10-29","pendingRemovalBy":null}]}
            """);

        var result = await fangbreakerZoneClient.GetTodayAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(BonusKey.Unconfirmed, result.Value!["Abysmal Sea"]);
        Assert.Equal(new BonusKey("experience"), result.Value["Bastion of Thunder"]);
    }

    [Fact]
    public async Task GetTodayToleratesADuplicateZone()
    {
        var fangbreakerZoneClient = Create(HttpStatusCode.OK, """
            {"ok":true,"data":[{"name":"Befallen","bonus":"loot"},{"name":"Befallen","bonus":"loot"}]}
            """);

        var result = await fangbreakerZoneClient.GetTodayAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(new BonusKey("loot"), result.Value!["Befallen"]);
    }

    [Fact]
    public async Task ReportBonus()
    {
        var fangbreakerZoneClient = Create(HttpStatusCode.OK, """
            {"ok":true,"data":{"updated":false,"zoneBonus":{"id":"f53fd95b-05e9-4e9f-968f-4e30c91d1020","serverId":1,"state":"ACTIVE"}}}
            """);

        var report = new BonusReport(Server.Frostreaver, "Bastion of Thunder", new BonusKey("experience"));
        var result = await fangbreakerZoneClient.ReportBonusAsync(report, Key, TestContext.Current.CancellationToken);

        Assert.Equal(ApiOutcome.Success, result.Outcome);
        Assert.False(result.Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ApiOutcome.Failed)]
    [InlineData(HttpStatusCode.Unauthorized, ApiOutcome.KeyRejected)]
    [InlineData(HttpStatusCode.Forbidden, ApiOutcome.Failed)]
    [InlineData(HttpStatusCode.NotFound, ApiOutcome.Failed)]
    [InlineData(HttpStatusCode.InternalServerError, ApiOutcome.Failed)]
    [InlineData(HttpStatusCode.BadGateway, ApiOutcome.Failed)]
    public async Task FailedStatus(HttpStatusCode httpStatusCode, ApiOutcome expected)
    {
        var fangbreakerZoneClient = Create(httpStatusCode, """{"ok":false,"error":"Server not found"}""");

        var report = new BonusReport(Server.Fangbreaker, "Befallen", BonusKey.None);
        var result = await fangbreakerZoneClient.ReportBonusAsync(report, Key, TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Outcome);
        Assert.Equal(
            $$"""POST https://fangbreaker.zone/api/client/bonuses returned {{(int)httpStatusCode}}: {"ok":false,"error":"Server not found"}""",
            result.Error);
    }

    [Fact]
    public async Task NetworkFailureFails()
    {
        var handler = new StubHttpHandler(new HttpRequestException("No such host is known."));
        var fangbreakerZoneClient = new FangbreakerZoneClient(handler);

        var result = await fangbreakerZoneClient.GetBonusTypesAsync(Server.Fangbreaker, TestContext.Current.CancellationToken);

        Assert.Equal(ApiOutcome.Failed, result.Outcome);
        Assert.Equal("GET https://fangbreaker.zone/api/servers/fangbreaker/bonuses/types failed: No such host is known.", result.Error);
    }

    [Fact]
    public void ApiKeyIsHiddenWhenFormatted()
    {
        Assert.Equal("key fz_live_****", $"key {Key}");
    }

    static FangbreakerZoneClient Create(HttpStatusCode httpStatusCode, string body) => new(new StubHttpHandler(httpStatusCode, body));
}
