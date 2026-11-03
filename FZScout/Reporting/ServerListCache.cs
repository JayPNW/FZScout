using FZScout.Scouting;
using FZScout.Site;

namespace FZScout.Reporting;

/// <summary>
/// Each server's <c>types</c> and <c>zones</c> lists, loaded when first needed.
/// If loading fails, it tries again the next time the lists are needed.
/// </summary>
class ServerListCache(FangbreakerZoneClient fangbreakerZoneClient, ConsoleReporter log)
{
    readonly Dictionary<Server, ServerLists> _lists = [];

    /// <summary>The lists, or null if they can't be loaded right now.</summary>
    public async Task<ServerLists?> GetAsync(Server server, CancellationToken cancellationToken = default)
    {
        if (!this._lists.ContainsKey(server))
        {
            await LoadAsync(server, cancellationToken);
        }

        return this._lists.GetValueOrDefault(server);
    }

    /// <summary>Forgets the lists and loads them again, as when the site says a zone wasn't found.</summary>
    public async Task ReloadAsync(Server server, CancellationToken cancellationToken = default)
    {
        this._lists.Remove(server);
        await LoadAsync(server, cancellationToken);
    }

    async Task LoadAsync(Server server, CancellationToken cancellationToken)
    {
        var types = await fangbreakerZoneClient.GetBonusTypesAsync(server, cancellationToken);
        if (!types.IsSuccess)
        {
            log.Error($"Couldn't load the {server} bonus types list. {types.Error}");
            return;
        }

        var zones = await fangbreakerZoneClient.GetZonesAsync(server, cancellationToken);
        if (!zones.IsSuccess)
        {
            log.Error($"Couldn't load the {server} zones list. {zones.Error}");
            return;
        }

        this._lists[server] = new ServerLists(types.Value!, zones.Value!);
    }
}
