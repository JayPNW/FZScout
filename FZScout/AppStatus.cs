namespace FZScout;

/// <summary>What the app is doing, as its icon and tooltip show it. Some of that changes just because time passes.</summary>
class AppStatus(TimeProvider timeProvider)
{
    const string ProblemDescription = "something went wrong; its window shows what";

    public bool IsScouting { get; set; }

    /// <summary>Whether the app has stopped on its own, like when the API key is rejected, and won't scout any more.</summary>
    public bool HasStopped { get; set; }

    /// <summary>Can already have passed, since the scouts only notice that when the next message arrives.</summary>
    public DateTimeOffset? WaitingForBonusMessageUntil { get; set; }

    public bool HadProblemSinceLastSuccess { get; set; }

    public StatusIcon Icon
    {
        get
        {
            if (HasStopped)
            {
                return StatusIcon.Stopped;
            }

            if (!IsScouting)
            {
                return StatusIcon.NotScouting;
            }

            if (IsWaitingForBonusMessage && HadProblemSinceLastSuccess)
            {
                return StatusIcon.WaitingForBonusMessageAfterProblem;
            }

            if (IsWaitingForBonusMessage)
            {
                return StatusIcon.WaitingForBonusMessage;
            }

            if (HadProblemSinceLastSuccess)
            {
                return StatusIcon.GemAfterProblem;
            }

            return StatusIcon.Gem;
        }
    }

    /// <summary>Short enough for a tray icon's tooltip.</summary>
    public string Description
    {
        get
        {
            if (HasStopped)
            {
                return "FZScout has stopped. Its window shows why.";
            }

            if (!IsScouting)
            {
                return "FZScout: not scouting yet";
            }

            if (IsWaitingForBonusMessage && HadProblemSinceLastSuccess)
            {
                return $"FZScout: {WaitingForBonusMessageDescription}, and {ProblemDescription}";
            }

            if (IsWaitingForBonusMessage)
            {
                return $"FZScout: {WaitingForBonusMessageDescription}";
            }

            if (HadProblemSinceLastSuccess)
            {
                return $"FZScout: {ProblemDescription}";
            }

            return "FZScout";
        }
    }

    public DateTimeOffset? ChangesByItselfAt
    {
        get
        {
            if (HasStopped)
            {
                return null;
            }

            if (IsWaitingForBonusMessage)
            {
                return WaitingForBonusMessageUntil;
            }

            return null;
        }
    }

    bool IsWaitingForBonusMessage => WaitingForBonusMessageUntil > timeProvider.GetUtcNow();

    string WaitingForBonusMessageDescription => $"waiting until {ToLocalTime(WaitingForBonusMessageUntil!.Value):HH:mm:ss} for a bonus message";

    DateTimeOffset ToLocalTime(DateTimeOffset time) => TimeZoneInfo.ConvertTime(time, timeProvider.LocalTimeZone);
}
