using System.Net;
using FZScout.Scouting;
using FZScout.Tests.Reporting;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests.Scouting;

public class ServerListCacheTests
{
    const string Types = "GET /api/servers/fangbreaker/bonuses/types";
    const string Zones = "GET /api/servers/fangbreaker/bonuses/zones";

    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    readonly FakeSite _site = new();
    readonly StringWriter _output = new();
    readonly ServerListCache _cache;

    public ServerListCacheTests()
    {
        this._cache = new ServerListCache(this._site.Client, new ConsoleReporter(this._output, new FakeTimeProvider(Start)));
    }

    static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LoadsTypesAndZones()
    {
        var lists = await this._cache.GetAsync(Server.Fangbreaker, Cancel);

        Assert.Equal(["experience", "loot", "none"], lists!.Types.Select(t => t.Value).Order());
        Assert.Equal(["Befallen", "Crushbone"], lists.Zones.Order());
    }

    [Fact]
    public async Task LoadsOnlyOnce()
    {
        await this._cache.GetAsync(Server.Fangbreaker, Cancel);
        await this._cache.GetAsync(Server.Fangbreaker, Cancel);

        Assert.Equal([Types, Zones], this._site.Requests);
    }

    [Fact]
    public async Task EachServerHasItsOwnLists()
    {
        await this._cache.GetAsync(Server.Fangbreaker, Cancel);
        await this._cache.GetAsync(Server.Frostreaver, Cancel);

        Assert.Equal(
            [Types, Zones, "GET /api/servers/frostreaver/bonuses/types", "GET /api/servers/frostreaver/bonuses/zones"],
            this._site.Requests);
    }

    [Fact]
    public async Task FailureIsLogged()
    {
        this._site.Fail(Zones, HttpStatusCode.InternalServerError);

        Assert.Null(await this._cache.GetAsync(Server.Fangbreaker, Cancel));
        Assert.Equal(
            """
            12:00:00 ERROR: Couldn't load the Fangbreaker zones list. GET https://fangbreaker.zone/api/servers/fangbreaker/bonuses/zones returned 500: {"ok":false,"error":"Nope"}

            """,
            this._output.ToString(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task AfterAFailureItTriesAgainWhenNextNeeded()
    {
        this._site.Fail(Types, HttpStatusCode.InternalServerError);
        await this._cache.GetAsync(Server.Fangbreaker, Cancel);
        Assert.Equal([Types], this._site.Requests);

        this._site.Succeed(Types);
        Assert.NotNull(await this._cache.GetAsync(Server.Fangbreaker, Cancel));
        Assert.Equal([Types, Types, Zones], this._site.Requests);
    }
}
