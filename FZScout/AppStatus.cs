using FZScout.Site;

namespace FZScout;

/// <summary>What the app is doing, as the UI shows it. Some of that changes just because time passes.</summary>
class AppStatus(TimeProvider timeProvider)
{
    const string ApiKeyRejectedDescription = "API Key rejected; enter a new one in settings";
    const int MaxTooltipLength = 127;
    const string ProblemDescription = "hit a problem. Open the main window for details.";

    public bool IsScouting { get; set; }

    /// <summary>Whether the app has stopped on its own, like after an unexpected problem, and won't scout any more.</summary>
    public bool HasStopped { get; set; }

    /// <summary>Can already have passed, since the scouts only notice that when the next message arrives.</summary>
    public DateTimeOffset? WaitingForBonusMessageUntil { get; set; }

    public bool IsEverQuestLoggingEnabled { get; set; } = true;

    public bool IsEverQuestRunning { get; set; }

    /// <summary>See <see cref="Scout.AnyCharacterLoggedInUntil"/>.</summary>
    public DateTimeOffset? AnyCharacterLoggedInUntil { get; set; }

    public bool HadProblemSinceLastSuccess { get; set; }

    public bool IsApiKeyRejected { get; set; }

    public bool IsUpdateDownloaded { get; set; }

    public BonusShownOnSite? BonusShownOnSiteForCurrentZone { get; set; }

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

            if (!IsEverQuestLoggingEnabled)
            {
                return ScoutingStatus.EverQuestLoggingDisabled;
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

            if (!IsEverQuestRunning || !IsAnyCharacterLoggedIn)
            {
                return ScoutingStatus.NotScouting;
            }

            if (IsUpdateDownloaded)
            {
                return ScoutingStatus.ScoutingWithUpdateDownloaded;
            }

            return ScoutingStatus.Scouting;
        }
    }

    /// <summary>Short enough for a tray icon's tooltip.</summary>
    public string Description
    {
        get
        {
            if (HasStopped || BonusShownOnSiteForCurrentZone is not { } shown)
            {
                return ScoutingDescription;
            }

            var withZoneAndSite = $"{ScoutingDescription}\n{shown.Zone}: '{shown.Bonus}' on {shown.Server.Site.Host}";
            if (withZoneAndSite.Length <= MaxTooltipLength)
            {
                return withZoneAndSite;
            }

            return $"{ScoutingDescription}\nSite shows '{shown.Bonus}'";
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

            return new[] { FutureOrNull(WaitingForBonusMessageUntil), FutureOrNull(CampFinishesAt) }.Min();
        }
    }

    string ScoutingDescription
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

            if (!IsEverQuestLoggingEnabled)
            {
                return "FZScout: EverQuest logging is off. Type `/log` on in EverQuest.";
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

            if (!IsEverQuestRunning)
            {
                return "FZScout: waiting for EverQuest to start";
            }

            if (!IsAnyCharacterLoggedIn)
            {
                return "FZScout: waiting for a character to log in";
            }

            if (IsUpdateDownloaded)
            {
                return "FZScout: update downloaded; it installs when FZScout exits";
            }

            return "FZScout";
        }
    }

    DateTimeOffset? CampFinishesAt
    {
        get
        {
            if (AnyCharacterLoggedInUntil == DateTimeOffset.MaxValue)
            {
                return null;
            }

            return AnyCharacterLoggedInUntil;
        }
    }

    bool IsAnyCharacterLoggedIn => AnyCharacterLoggedInUntil > timeProvider.GetUtcNow();

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

    DateTimeOffset? FutureOrNull(DateTimeOffset? time)
    {
        if (time > timeProvider.GetUtcNow())
        {
            return time;
        }

        return null;
    }

    DateTimeOffset ToLocalTime(DateTimeOffset time) => TimeZoneInfo.ConvertTime(time, timeProvider.LocalTimeZone);
}
