using FZScout.Logs;
using FZScout.Reporting;
using FZScout.Scouting;

namespace FZScout;

/// <summary>Connects the pieces: log events go to each character's scout, and what the scouts decide goes to the reporter.</summary>
class Scout(LogWatcher logWatcher, ServerListCache serverListCache, BonusReporter bonusReporter, Func<IReadOnlyCollection<EqProcess>> findEqProcesses)
{
    readonly Dictionary<Character, CharacterScout> _characters = [];
    DateTimeOffset? _nextScan;

    public LogWatcher Watcher => logWatcher;

    /// <summary>Call about once a second.</summary>
    public async Task TickAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (this._nextScan is null || now >= this._nextScan)
        {
            foreach (var character in logWatcher.Scan(findEqProcesses(), now))
            {
                // EverQuest stopped, so the character can't see a bonus message; don't let a timer report a false 'none'.
                this._characters.Remove(character);
            }

            this._nextScan = now + LogWatcher.ScanInterval;
        }

        foreach (var logEvent in logWatcher.Poll())
        {
            var serverLists = await serverListCache.GetAsync(logEvent.Character.Server, now, cancellationToken);
            var actions = ScoutFor(logEvent.Character).Handle(logEvent.Event, now, serverLists);
            await ExecuteAsync(actions, now, cancellationToken);
        }

        foreach (var scout in this._characters.Values)
        {
            await ExecuteAsync(scout.Tick(now, serverListCache.Peek(scout.Character.Server)), now, cancellationToken);
        }

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
