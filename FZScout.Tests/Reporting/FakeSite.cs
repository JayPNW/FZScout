using System.Net;
using FZScout.Site;

namespace FZScout.Tests.Reporting;

/// <summary>
/// An in-memory fangbreaker.zone/frostreaver.zone at the HTTP boundary, so the real client runs in tests.
/// Every route succeeds unless told otherwise.
/// </summary>
class FakeSite : HttpMessageHandler
{
    readonly Dictionary<string, HttpStatusCode> _failures = [];
    readonly Dictionary<string, string> _today = [];

    /// <summary>Each request as <c>GET /path</c> or <c>POST /path {body}</c>.</summary>
    public List<string> Requests { get; } = [];

    public FangbreakerZoneClient Client => new(new HttpClient(this));

    /// <summary>Fails every request to a route like <c>POST /api/client/bonuses</c> with this status.</summary>
    public void Fail(string route, HttpStatusCode httpStatusCode) => this._failures[route] = httpStatusCode;

    public void Succeed(string route) => this._failures.Remove(route);

    public void SetToday(string zone, string bonus) => this._today[zone] = bonus;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var route = $"{request.Method} {request.RequestUri!.AbsolutePath}";
        var body = string.Empty;
        if (request.Content is not null)
        {
            body = $" {await request.Content.ReadAsStringAsync(cancellationToken)}";
        }

        Requests.Add($"{route}{body}");

        if (this._failures.TryGetValue(route, out var httpStatusCode))
        {
            return new HttpResponseMessage(httpStatusCode) { Content = new StringContent("""{"ok":false,"error":"Nope"}""") };
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BodyFor(route)) };
    }

    string BodyFor(string route)
    {
        if (route.EndsWith("/types", StringComparison.Ordinal))
        {
            return """{"ok":true,"data":[{"key":"experience"},{"key":"loot"},{"key":"none"}]}""";
        }

        if (route.EndsWith("/zones", StringComparison.Ordinal))
        {
            return """{"ok":true,"data":[{"name":"Befallen"},{"name":"Crushbone"}]}""";
        }

        if (route.EndsWith("/today", StringComparison.Ordinal))
        {
            var zones = this._today.Select(z => $$"""{"name":"{{z.Key}}","bonus":"{{z.Value}}"}""");
            return $$"""{"ok":true,"data":[{{string.Join(",", zones)}}]}""";
        }

        return """{"ok":true,"data":{"updated":true,"zoneBonus":{}}}""";
    }
}
