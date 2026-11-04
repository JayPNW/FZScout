using FZScout.Reporting;
using FZScout.Scouting;

namespace FZScout;

class Scout(ServerListCache serverListCache, Log log, BonusReporter bonusReporter)
{
    readonly Dictionary<Character, CharacterScout> _characters = [];

    public event Action<DateTimeOffset?>? WaitingForBonusMessageUntilChanged;

    /// <summary>Like <see cref="CharacterScout.WaitingForBonusMessageUntil"/>, this can already have passed.</summary>
    public DateTimeOffset? WaitingForBonusMessageUntil => this._characters.Values.Max(character => character.WaitingForBonusMessageUntil);

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
        var waitingForBonusMessageUntilBefore = WaitingForBonusMessageUntil;
        await scout.HandleAsync(logEvent, now, serverLists, cancellationToken);
        if (WaitingForBonusMessageUntil != waitingForBonusMessageUntilBefore)
        {
            WaitingForBonusMessageUntilChanged?.Invoke(WaitingForBonusMessageUntil);
        }
    }
}
