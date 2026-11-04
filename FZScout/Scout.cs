using FZScout.Reporting;
using FZScout.Scouting;

namespace FZScout;

/// <summary>Connects the pieces: log events go to each character's scout, along with that server's lists.</summary>
class Scout(ServerListCache serverListCache, ConsoleReporter log, BonusReporter bonusReporter)
{
    readonly Dictionary<Character, CharacterScout> _characters = [];

    /// <summary>
    /// Call for every event the EverQuestLogWatcher library reports. Servers we don't report, and events that don't matter to scouting, are ignored.
    /// A character whose deadline for a bonus message passed before this message arrived gets that report too, folded into the same call.
    /// </summary>
    public async Task HandleAsync(EverQuestLogWatcher.EverQuestLogEvent logEvent, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var server = Server.FromName(logEvent.Character.Server);
        if (server is null)
        {
            return;
        }

        var character = new Character(logEvent.Character.Name, server);
        var serverLists = await serverListCache.GetAsync(character.Server, cancellationToken);
        var scout = this._characters.GetOrAdd(character, new CharacterScout(character, log, bonusReporter));
        await scout.HandleAsync(logEvent, now, serverLists, cancellationToken);
    }
}
