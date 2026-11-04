namespace FZScout;

/// <summary>FZScout's emerald gem, and the other <see cref="StatusIcon"/>s, drawn by scripts/draw-icons.ps1.</summary>
static class AppIcon
{
    public static Icon Load() => Load(StatusIcon.Gem);

    /// <summary>All sizes; Windows picks the one it needs.</summary>
    public static Icon Load(StatusIcon statusIcon)
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName(statusIcon))!;
        return new Icon(stream);
    }

    /// <summary>The size drawn for the system tray at the current display scale, rather than a larger one shrunk to fit.</summary>
    public static Icon LoadForTray(StatusIcon statusIcon)
    {
        using var icon = Load(statusIcon);
        return new Icon(icon, SystemInformation.SmallIconSize);
    }

    static string ResourceName(StatusIcon statusIcon) => statusIcon switch
    {
        StatusIcon.NotScouting => "FZScoutNotScouting.ico",
        StatusIcon.Stopped => "FZScoutStopped.ico",
        StatusIcon.Gem => "FZScout.ico",
        StatusIcon.GemAfterProblem => "FZScoutProblem.ico",
        StatusIcon.WaitingForBonusMessage => "FZScoutWaiting.ico",
        StatusIcon.WaitingForBonusMessageAfterProblem => "FZScoutWaitingProblem.ico",
        _ => throw new ArgumentOutOfRangeException(nameof(statusIcon), statusIcon, null),
    };
}
