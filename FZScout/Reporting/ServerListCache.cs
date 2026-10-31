using FZScout.Scouting;
using FZScout.Site;

namespace FZScout.Reporting;

/// <summary>
/// Each server's <c>types</c> and <c>zones</c> lists, loaded when first needed.
/// If loading fails, it retries once after a minute; if that fails too, it tries again the next time the lists are needed.
/// </summary>
class ServerListCache(FangbreakerZoneClient fangbreakerZoneClient, ConsoleLog log)
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    readonly Dictionary<Server, ServerLists> _lists = [];
    readonly Dictionary<Server, DateTimeOffset> _retryAt = [];

    /// <summary>The lists, or null if they can't be loaded right now.</summary>
    public async Task<ServerLists?> GetAsync(Server server, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (this._lists.TryGetValue(server, out var lists))
        {
            return lists;
        }

        if (this._retryAt.ContainsKey(server))
        {
            return null;
        }

        if (!await LoadAsync(server, cancellationToken))
        {
            this._retryAt[server] = now + RetryDelay;
        }

        return this._lists.GetValueOrDefault(server);
    }

    /// <summary>The lists if they're already loaded. Never loads them.</summary>
    public ServerLists? Peek(Server server) => this._lists.GetValueOrDefault(server);

    /// <summary>Forgets the lists and loads them again, as when the site says a zone wasn't found.</summary>
    public async Task ReloadAsync(Server server, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        this._lists.Remove(server);
        this._retryAt.Remove(server);
        await GetAsync(server, now, cancellationToken);
    }

    public async Task TickAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var due = this._retryAt.Where(r => r.Value <= now).Select(r => r.Key).ToList();
        foreach (var server in due)
        {
            this._retryAt.Remove(server);
            await LoadAsync(server, cancellationToken);
        }
    }

    async Task<bool> LoadAsync(Server server, CancellationToken cancellationToken)
    {
        var types = await fangbreakerZoneClient.GetBonusTypesAsync(server, cancellationToken);
        if (!types.IsSuccess)
        {
            log.Error($"Couldn't load the {server} bonus types list. {types.Error}");
            return false;
        }

        var zones = await fangbreakerZoneClient.GetZonesAsync(server, cancellationToken);
        if (!zones.IsSuccess)
        {
            log.Error($"Couldn't load the {server} zones list. {zones.Error}");
            return false;
        }

        this._lists[server] = new ServerLists(types.Value!, zones.Value!);
        return true;
    }
}
