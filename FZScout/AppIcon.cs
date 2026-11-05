using System.Drawing;
using System.Runtime.InteropServices;

namespace FZScout;

/// <summary>FZScout's emerald gem, and an icon for each <see cref="ScoutingStatus"/>, drawn by scripts/draw-icons.ps1.</summary>
static class AppIcon
{
    const string GemResourceName = "FZScout.ico";

    const int SmallIconWidthMetric = 49;

    const int SmallIconHeightMetric = 50;

    /// <summary>All sizes; Windows picks the one it needs.</summary>
    public static Icon Load(ScoutingStatus scoutingStatus)
    {
        using var stream = OpenResource(scoutingStatus);
        return new Icon(stream);
    }

    /// <summary>The size drawn for the system tray at the current display scale, rather than a larger one shrunk to fit.</summary>
    public static Icon LoadForTray(ScoutingStatus scoutingStatus)
    {
        using var icon = Load(scoutingStatus);
        return new Icon(icon, GetSystemMetrics(SmallIconWidthMetric), GetSystemMetrics(SmallIconHeightMetric));
    }

    public static Stream OpenGemResource() => OpenResource(GemResourceName);

    public static Stream OpenResource(ScoutingStatus scoutingStatus) => OpenResource(ResourceName(scoutingStatus));

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

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int metric);

    static Stream OpenResource(string resourceName) => typeof(AppIcon).Assembly.GetManifestResourceStream(resourceName)!;
}
