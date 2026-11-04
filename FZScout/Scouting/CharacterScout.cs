using FZScout.Site;

namespace FZScout.Scouting;

/// <summary>
/// The business rules for one character: turns their log events and the passing of time into reports.
/// See the Timing and Reporting sections of InternalDocumentation/SPEC.md.
/// </summary>
class CharacterScout(Character character, Func<ScoutAction, CancellationToken, Task> executeAsync)
{
    /// <summary>EverQuest announces a bonus 3 to 3.5 minutes after entry when there's no kill, so after 5 there is none.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(5);

    /// <summary>A kill that triggers the bonus message gets it in the same second.</summary>
    public static readonly TimeSpan KillTimeout = TimeSpan.FromSeconds(5);

    public const string ReportIssuesUrl = $"https://github.com/{GitHubRepository.OwnerAndName}/issues/new";

    bool _alreadyReported;

    DateTimeOffset? _idleDeadline;

    DateTimeOffset? _killDeadline;

    public Zone? CurrentZone { get; private set; }

    /// <param name="lists">The server's lists, or null if they couldn't be loaded.</param>
    public async Task HandleAsync(EverQuestLogWatcher.EverQuestLogEvent logEvent, DateTimeOffset now, ServerLists? lists, CancellationToken cancellationToken = default)
    {
        ResolveZone(lists);
        if (ReportNoneIfADeadlinePassedBeforeThisMessage(now) is { } reportNoneAction)
        {
            await executeAsync(reportNoneAction, cancellationToken);
        }
        if (DispatchLogEvent(logEvent, now, lists) is { } logEventAction)
        {
            await executeAsync(logEventAction, cancellationToken);
        }
    }

    /// <summary>
    /// There's no clock: a deadline is only noticed when the character's next message arrives.
    /// A character who stops logging in before any further message arrives never gets a false <c>none</c>.
    /// </summary>
    ScoutAction? ReportNoneIfADeadlinePassedBeforeThisMessage(DateTimeOffset now)
    {
        if (!IsDue(this._killDeadline, now) && !IsDue(this._idleDeadline, now))
        {
            return null;
        }

        if (CurrentZone is UnresolvedZone unresolved)
        {
            MarkAlreadyReportedThisZone();
            return new LogErrorAction($"{character} was in '{unresolved.LogName}', but the {character.Server} zones list still isn't loaded, so 'none' wasn't reported.");
        }

        if (CurrentZone is not KnownZone zone)
        {
            return null;
        }

        MarkAlreadyReportedThisZone();
        return new ReportNoBonusAction(character.Server, zone.SiteName);
    }

    static bool IsDue(DateTimeOffset? deadline, DateTimeOffset now) => deadline is not null && deadline <= now;

    static bool IsReportable(Zone? zone) => zone is KnownZone or UnresolvedZone;

    ScoutAction? DispatchLogEvent(EverQuestLogWatcher.EverQuestLogEvent logEvent, DateTimeOffset now, ServerLists? lists) => logEvent switch
    {
        EverQuestLogWatcher.ZoneEntered e => OnZoneEntered(e.ZoneName, now, lists),
        EverQuestLogWatcher.MobSlainByYou => OnKill(now),
        EverQuestLogWatcher.SlainByPet e when e.OwnerName == character.Name => OnKill(now),
        EverQuestLogWatcher.SlainByWarder e when e.OwnerName == character.Name => OnKill(now),
        EverQuestLogWatcher.AreaIsSaturatedWithUnstableMagic e => OnBonus(e.Bonus, lists),
        EverQuestLogWatcher.DailyResetLogEvent => OnDailyReset(now),
        EverQuestLogWatcher.CampingCountdownProgressed => OnCamping(),
        _ => null,
    };

    /// <summary>Once the zones list loads, works out which zone an <see cref="UnresolvedZone"/> is. Its deadlines still stand.</summary>
    void ResolveZone(ServerLists? lists)
    {
        if (CurrentZone is not UnresolvedZone unresolved || lists is null)
        {
            return;
        }

        CurrentZone = ZoneNames.Resolve(unresolved.LogName, lists.Zones);
        if (CurrentZone is not KnownZone)
        {
            MarkAlreadyReportedThisZone();
        }
    }

    ScoutAction? OnZoneEntered(string logZone, DateTimeOffset now, ServerLists? lists)
    {
        CurrentZone = ZoneNames.Resolve(logZone, lists?.Zones);
        this._alreadyReported = false;
        this._killDeadline = null;
        this._idleDeadline = null;

        if (IsReportable(CurrentZone))
        {
            var deadline = SetIdleDeadline(now);
            return new LogInfoAction($"{character} entered {logZone}, waiting until {deadline.ToLocalTime():HH:mm:ss} for a bonus message.");
        }

        return new LogInfoAction($"{character} entered {logZone}.");
    }

    ScoutAction? OnKill(DateTimeOffset now)
    {
        if (IsReportable(CurrentZone) && !this._alreadyReported && this._killDeadline is null)
        {
            this._killDeadline = now + KillTimeout;
            return WaitingUntil(this._killDeadline.Value);
        }

        return null;
    }

    ScoutAction? OnBonus(string announcementText, ServerLists? lists)
    {
        if (BonusKey.FromAnnouncementText(announcementText) is not { } bonus)
        {
            return null;
        }

        // An unknown zone that never gets a bonus, like The Plane of Knowledge, is fine. One that does needs adding.
        if (CurrentZone is UnknownZone unknown)
        {
            return new LogErrorAction($"{character} saw a '{bonus}' bonus in '{unknown.LogName}', which isn't on the {character.Server} zones list. Please report it at {ReportIssuesUrl}");
        }

        if (CurrentZone is UnresolvedZone unresolved)
        {
            MarkAlreadyReportedThisZone();
            return new LogErrorAction($"{character} saw a '{bonus}' bonus in '{unresolved.LogName}', but the {character.Server} zones list isn't loaded, so it wasn't reported.");
        }

        if (CurrentZone is not KnownZone zone)
        {
            return null;
        }

        if (lists is not null && !lists.Types.Contains(bonus))
        {
            MarkAlreadyReportedThisZone();
            return new LogErrorAction($"{character} saw a '{bonus}' bonus in '{zone.SiteName}', which isn't on the {character.Server} bonus types list. Please report it at {ReportIssuesUrl}");
        }

        MarkAlreadyReportedThisZone();
        return new ReportBonusAction(new BonusReport(character.Server, zone.SiteName, bonus));
    }

    ScoutAction? OnDailyReset(DateTimeOffset now)
    {
        if (!IsReportable(CurrentZone))
        {
            return null;
        }

        this._alreadyReported = false;
        this._killDeadline = null;
        return WaitingUntil(SetIdleDeadline(now));
    }

    /// <summary>The character is probably logging out, and the next message may be from much later, so stop waiting for a bonus.</summary>
    ScoutAction? OnCamping()
    {
        this._idleDeadline = null;
        return null;
    }

    LogInfoAction WaitingUntil(DateTimeOffset deadline)
    {
        return new($"{character} waiting until {deadline.ToLocalTime():HH:mm:ss} for a bonus message.");
    }

    DateTimeOffset SetIdleDeadline(DateTimeOffset now)
    {
        var deadline = now + IdleTimeout;
        this._idleDeadline = deadline;
        return deadline;
    }

    void MarkAlreadyReportedThisZone()
    {
        this._alreadyReported = true;
        this._killDeadline = null;
        this._idleDeadline = null;
    }
}
