using FZScout.Reporting;
using FZScout.Scouting;

namespace FZScout;

/// <summary>Connects the pieces: log events go to each character's scout, and what the scouts decide goes to the reporter.</summary>
class Scout(ServerListCache serverListCache, BonusReporter bonusReporter)
{
    readonly Dictionary<Character, CharacterScout> _characters = [];

    /// <summary>
    /// Call for every event the EverQuestLogWatcher library reports. Servers we don't report, and events that don't matter to scouting, are ignored.
    /// A character whose bonus/none timer ran out before this message arrived gets that report too, folded into the same call,
    /// as do retries that came due.
    /// </summary>
    public async Task HandleAsync(EverQuestLogWatcher.EverQuestLogEvent logEvent, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await RetryDueWorkAsync(now, cancellationToken);

        var server = Server.FromSlug(logEvent.Character.Server);
        if (server is null)
        {
            return;
        }

        var character = new Character(logEvent.Character.Name, server);
        var serverLists = await serverListCache.GetAsync(character.Server, now, cancellationToken);
        var scout = ScoutFor(character);
        IReadOnlyList<ScoutAction> actions = [.. scout.Tick(now, serverLists), .. scout.Handle(logEvent, now, serverLists)];
        await ExecuteAsync(actions, now, cancellationToken);
    }

    async Task RetryDueWorkAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await serverListCache.TickAsync(now, cancellationToken);
        await bonusReporter.TickAsync(now, cancellationToken);
    }

    CharacterScout ScoutFor(Character character)
    {
        if (!this._characters.TryGetValue(character, out var scout))
        {
            scout = new CharacterScout(character);
            this._characters[character] = scout;
        }

        return scout;
    }

    async Task ExecuteAsync(IReadOnlyList<ScoutAction> actions, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var action in actions)
        {
            await bonusReporter.ExecuteAsync(action, now, cancellationToken);
        }
    }
}
