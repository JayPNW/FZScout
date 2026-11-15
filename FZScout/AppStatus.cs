namespace FZScout;

/// <summary>What the app is doing, as the UI shows it. Some of that changes just because time passes.</summary>
class AppStatus(TimeProvider timeProvider)
{
    const string ApiKeyRejectedDescription = "API Key rejected; enter a new one in settings";
    const string ProblemDescription = "hit a problem. Open the main window for details.";

    public bool IsScouting { get; set; }

    /// <summary>Whether the app has stopped on its own, like after an unexpected problem, and won't scout any more.</summary>
    public bool HasStopped { get; set; }

    /// <summary>Can already have passed, since the scouts only notice that when the next message arrives.</summary>
    public DateTimeOffset? WaitingForBonusMessageUntil { get; set; }

    public bool HadProblemSinceLastSuccess { get; set; }

    public bool IsApiKeyRejected { get; set; }

    public ScoutingStatus ScoutingStatus
    {
        get
        {
            if (HasStopped)
            {
                return ScoutingStatus.Stopped;
            }

            if (!IsScouting)
            {
                return ScoutingStatus.NotScouting;
            }

            if (IsWaitingForBonusMessage && HadProblemSinceLastSuccess)
            {
                return ScoutingStatus.WaitingForBonusMessageAfterProblem;
            }

            if (IsWaitingForBonusMessage)
            {
                return ScoutingStatus.WaitingForBonusMessage;
            }

            if (HadProblemSinceLastSuccess)
            {
                return ScoutingStatus.ScoutingAfterProblem;
            }

            return ScoutingStatus.Scouting;
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
                return $"FZScout: {WaitingForBonusMessageDescription}; {MostUsefulProblemDescription}";
            }

            if (IsWaitingForBonusMessage)
            {
                return $"FZScout: {WaitingForBonusMessageDescription}";
            }

            if (HadProblemSinceLastSuccess)
            {
                return $"FZScout: {MostUsefulProblemDescription}";
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

    string MostUsefulProblemDescription
    {
        get
        {
            if (IsApiKeyRejected)
            {
                return ApiKeyRejectedDescription;
            }

            return ProblemDescription;
        }
    }

    bool IsWaitingForBonusMessage => WaitingForBonusMessageUntil > timeProvider.GetUtcNow();

    string WaitingForBonusMessageDescription => $"waiting until {ToLocalTime(WaitingForBonusMessageUntil!.Value):HH:mm:ss} for a bonus message";

    DateTimeOffset ToLocalTime(DateTimeOffset time) => TimeZoneInfo.ConvertTime(time, timeProvider.LocalTimeZone);
}
