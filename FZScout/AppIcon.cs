namespace FZScout;

/// <summary>FZScout's emerald gem, drawn by scripts/draw-icon.ps1.</summary>
static class AppIcon
{
    /// <summary>All sizes; Windows picks the one it needs.</summary>
    public static Icon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("FZScout.ico")!;
        return new Icon(stream);
    }

    /// <summary>The size drawn for the system tray at the current display scale, rather than a larger one shrunk to fit.</summary>
    public static Icon LoadForTray()
    {
        using var icon = Load();
        return new Icon(icon, SystemInformation.SmallIconSize);
    }
}
