using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FZScout;

/// <summary><see cref="AppIcon"/>'s icons, for WPF windows. Every size is kept, so Windows picks the one it needs.</summary>
static class WindowIcon
{
    /// <summary>Decoded on each call, because an <see cref="IconBitmapDecoder"/> can only be used on the thread that created it.</summary>
    public static ImageSource Gem => Decode(AppIcon.OpenGemResource());

    /// <summary>Decoded on each call, because an <see cref="IconBitmapDecoder"/> can only be used on the thread that created it.</summary>
    public static ImageSource For(ScoutingStatus scoutingStatus) => Decode(AppIcon.OpenResource(scoutingStatus));

    static ImageSource Decode(Stream stream)
    {
        using (stream)
        {
            return new IconBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        }
    }
}
