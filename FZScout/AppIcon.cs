namespace FZScout;

/// <summary>FZScout's emerald gem, and the emerald hourglass shown while waiting for a bonus message, drawn by scripts/draw-icons.ps1.</summary>
static class AppIcon
{
    /// <summary>All sizes; Windows picks the one it needs.</summary>
    public static Icon Load() => LoadResource("FZScout.ico");

    /// <summary>The size drawn for the system tray at the current display scale, rather than a larger one shrunk to fit.</summary>
    public static Icon LoadForTray() => ForTray(Load());

    /// <summary>Like <see cref="LoadForTray"/>, but the hourglass.</summary>
    public static Icon LoadWaitingForBonusMessageForTray() => ForTray(LoadResource("FZScoutWaiting.ico"));

    static Icon LoadResource(string name)
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(name)!;
        return new Icon(stream);
    }

    static Icon ForTray(Icon allSizes)
    {
        using (allSizes)
        {
            return new Icon(allSizes, SystemInformation.SmallIconSize);
        }
    }
}
