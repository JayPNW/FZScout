namespace FZScout;

/// <summary>What the app is doing, as its icon and tooltip show it. Some of that changes just because time passes.</summary>
class AppStatus(TimeProvider timeProvider)
{
    /// <summary>Whether the app has an accepted API key and is watching the logs.</summary>
    public bool IsScouting { get; set; }

    /// <summary>
    /// When the last character stops waiting for a bonus message, or null if none are waiting.
    /// Can already have passed, since the scouts only notice that when the next message arrives.
    /// </summary>
    public DateTimeOffset? WaitingForBonusMessageUntil { get; set; }

    public StatusIcon Icon
    {
        get
        {
            if (!IsScouting)
            {
                return StatusIcon.NotScouting;
            }

            if (IsWaitingForBonusMessage)
            {
                return StatusIcon.WaitingForBonusMessage;
            }

            return StatusIcon.Gem;
        }
    }

    /// <summary>Short enough for a tray icon's tooltip.</summary>
    public string Description
    {
        get
        {
            if (!IsScouting)
            {
                return "FZScout: not scouting yet";
            }

            if (IsWaitingForBonusMessage)
            {
                return $"FZScout: waiting until {ToLocalTime(WaitingForBonusMessageUntil!.Value):HH:mm:ss} for a bonus message";
            }

            return "FZScout";
        }
    }

    /// <summary>When the icon or description will next change just because time passes, or null if they won't.</summary>
    public DateTimeOffset? ChangesByItselfAt
    {
        get
        {
            if (IsWaitingForBonusMessage)
            {
                return WaitingForBonusMessageUntil;
            }

            return null;
        }
    }

    bool IsWaitingForBonusMessage => WaitingForBonusMessageUntil > timeProvider.GetUtcNow();

    DateTimeOffset ToLocalTime(DateTimeOffset time) => TimeZoneInfo.ConvertTime(time, timeProvider.LocalTimeZone);
}
