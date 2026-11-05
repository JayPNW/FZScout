namespace FZScout;

/// <summary>FZScout's emerald gem, and an icon for each <see cref="ScoutingStatus"/>, drawn by scripts/draw-icons.ps1.</summary>
static class AppIcon
{
    public static Icon Load() => Load(ScoutingStatus.Scouting);

    /// <summary>All sizes; Windows picks the one it needs.</summary>
    public static Icon Load(ScoutingStatus scoutingStatus)
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName(scoutingStatus))!;
        return new Icon(stream);
    }

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
        ScoutingStatus.Scouting => "FZScout.ico",
        ScoutingStatus.ScoutingAfterProblem => "FZScoutProblem.ico",
        ScoutingStatus.WaitingForBonusMessage => "FZScoutWaiting.ico",
        ScoutingStatus.WaitingForBonusMessageAfterProblem => "FZScoutWaitingProblem.ico",
        _ => throw new ArgumentOutOfRangeException(nameof(scoutingStatus), scoutingStatus, null),
    };
}
