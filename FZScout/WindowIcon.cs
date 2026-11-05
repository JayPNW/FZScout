using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FZScout;

/// <summary><see cref="AppIcon"/>'s icons, for WPF windows. Every size is kept, so Windows picks the one it needs.</summary>
static class WindowIcon
{
    static readonly Dictionary<ScoutingStatus, ImageSource> ForScoutingStatus = Enum.GetValues<ScoutingStatus>().ToDictionary(scoutingStatus => scoutingStatus, scoutingStatus => Decode(AppIcon.OpenResource(scoutingStatus)));

    public static ImageSource Gem { get; } = Decode(AppIcon.OpenGemResource());

    public static ImageSource For(ScoutingStatus scoutingStatus) => ForScoutingStatus[scoutingStatus];

    static ImageSource Decode(Stream stream)
    {
        using (stream)
        {
            return new IconBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        }
    }
}
