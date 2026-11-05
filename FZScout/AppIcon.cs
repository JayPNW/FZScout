namespace FZScout;

/// <summary>FZScout's emerald gem, and an icon for each <see cref="ScoutingStatus"/>, drawn by scripts/draw-icons.ps1.</summary>
static class AppIcon
{
    const string GemResourceName = "FZScout.ico";

    public static Icon Load() => LoadResource(GemResourceName);

    /// <summary>All sizes; Windows picks the one it needs.</summary>
    public static Icon Load(ScoutingStatus scoutingStatus) => LoadResource(ResourceName(scoutingStatus));

    /// <summary>The size drawn for the system tray at the current display scale, rather than a larger one shrunk to fit.</summary>
    public static Icon LoadForTray(ScoutingStatus scoutingStatus)
    {
        using var icon = Load(scoutingStatus);
        return new Icon(icon, SystemInformation.SmallIconSize);
    }

    static string ResourceName(ScoutingStatus scoutingStatus) => scoutingStatus switch
    {
        ScoutingStatus.NotScouting => "FZScoutNotScouting.ico",
        ScoutingStatus.Stopped => "FZScoutStopped.ico",
        ScoutingStatus.Scouting => GemResourceName,
        ScoutingStatus.ScoutingAfterProblem => "FZScoutProblem.ico",
        ScoutingStatus.WaitingForBonusMessage => "FZScoutWaiting.ico",
        ScoutingStatus.WaitingForBonusMessageAfterProblem => "FZScoutWaitingProblem.ico",
        _ => throw new ArgumentOutOfRangeException(nameof(scoutingStatus), scoutingStatus, null),
    };

    /// <summary>All sizes; Windows picks the one it needs.</summary>
    static Icon LoadResource(string resourceName)
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(resourceName)!;
        return new Icon(stream);
    }
}
