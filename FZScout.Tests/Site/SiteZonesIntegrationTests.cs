using FZScout.Site;

namespace FZScout.Tests.Site;

// Talks to the real sites.
// Fails when a site adds, removes, or renames a zone, so Zones.md and ZoneNames can be updated.
[CategoryTrait("Integration")]
public class SiteZonesIntegrationTests
{
    [Fact]
    public async Task KnownZones()
    {
        using var httpClient = new HttpClient();
        var fangbreakerZoneClient = new FangbreakerZoneClient(httpClient);

        var lists = new List<(Server Server, IReadOnlySet<string> Zones)>();
        foreach (var server in Server.All)
        {
            var result = await fangbreakerZoneClient.GetZonesAsync(server, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess, result.Error);
            lists.Add((server, result.Value!));
        }

        var zones = lists.SelectMany(l => l.Zones).Distinct().Order();

        await Verify(string.Join(Environment.NewLine, zones));
    }
}
