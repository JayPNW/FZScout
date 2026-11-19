using FZScout.Reporting;
using FZScout.Scouting;
using FZScout.Site;

namespace FZScout;

class Scout(ServerListCache serverListCache, Log log, BonusReporter bonusReporter, FangbreakerZoneClient fangbreakerZoneClient)
{
    readonly Dictionary<Character, CharacterScout> _characters = [];

    public event Action<DateTimeOffset?>? WaitingForBonusMessageUntilChanged;

    /// <summary>For the zone of the character with the latest message; null when that zone isn't on the site or the site couldn't be reached.</summary>
    public event Action<BonusShownOnSite?>? BonusShownOnSiteForCurrentZoneChanged;

    public event Action<DateTimeOffset?>? AnyCharacterLoggedInUntilChanged;

    /// <summary>The latest of the characters' <see cref="CharacterScout.LoggedInUntil"/>, so null when none has logged in.</summary>
    public DateTimeOffset? AnyCharacterLoggedInUntil => this._characters.Values.Max(character => character.LoggedInUntil);

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
        var zoneBefore = scout.CurrentZone;
        var characterWaitingForBonusMessageUntilBefore = scout.WaitingForBonusMessageUntil;
        var anyCharacterLoggedInUntilBefore = AnyCharacterLoggedInUntil;
        await scout.HandleAsync(logEvent, now, serverLists, cancellationToken);
        NotifyIfAnyCharacterLoggedInUntilChanged(anyCharacterLoggedInUntilBefore);
        if (WaitingForBonusMessageUntil != waitingForBonusMessageUntilBefore)
        {
            WaitingForBonusMessageUntilChanged?.Invoke(WaitingForBonusMessageUntil);
        }

        var characterStoppedWaitingForBonusMessage = characterWaitingForBonusMessageUntilBefore is not null && scout.WaitingForBonusMessageUntil is null;
        var siteMayShowSomethingNew = scout.CurrentZone != zoneBefore || characterStoppedWaitingForBonusMessage || logEvent is EverQuestLogWatcher.DailyResetLogEvent;
        if (siteMayShowSomethingNew && BonusShownOnSiteForCurrentZoneChanged is { } notifyBonusShownOnSiteChanged)
        {
            notifyBonusShownOnSiteChanged(await GetBonusShownOnSiteAsync(server, scout.CurrentZone, cancellationToken));
        }
    }

    /// <summary>For when EverQuest exits, since that logs nothing.</summary>
    public void ForgetCharactersBeingLoggedIn()
    {
        var anyCharacterLoggedInUntilBefore = AnyCharacterLoggedInUntil;
        foreach (var character in this._characters.Values)
        {
            character.ForgetBeingLoggedIn();
        }

        NotifyIfAnyCharacterLoggedInUntilChanged(anyCharacterLoggedInUntilBefore);
    }

    void NotifyIfAnyCharacterLoggedInUntilChanged(DateTimeOffset? anyCharacterLoggedInUntilBefore)
    {
        if (AnyCharacterLoggedInUntil != anyCharacterLoggedInUntilBefore)
        {
            AnyCharacterLoggedInUntilChanged?.Invoke(AnyCharacterLoggedInUntil);
        }
    }

    async Task<BonusShownOnSite?> GetBonusShownOnSiteAsync(Server server, Zone? zone, CancellationToken cancellationToken)
    {
        if (zone is not KnownZone knownZone)
        {
            return null;
        }

        var today = await fangbreakerZoneClient.GetTodayAsync(server, cancellationToken);
        if (!today.IsSuccess)
        {
            return null;
        }

        return new BonusShownOnSite(server, knownZone.SiteName, today.Value!.GetValueOrDefault(knownZone.SiteName, BonusKey.Unconfirmed));
    }
}
