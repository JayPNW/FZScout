using System.Windows;
using System.Windows.Media;

namespace FZScout.Tests.Xaml;

/// <summary>
/// Screenshots of the same window differ between machines unless these are pinned, so CI's don't match a dev machine's.
/// </summary>
static class MachineIndependentRendering
{
    /// <summary>The highlight color on a typical Windows 11 dev machine; CI's Windows Server is a shade different.</summary>
    static Color WindowsHighlightColor { get; } = Color.FromRgb(0, 120, 215);

    /// <summary>Narrow enough that WPF doesn't shrink the window to fit a small screen, like CI's 1024 pixels.</summary>
    public const double WidthThatFitsOnAnyScreen = 1000;

    public static T WithPinnedSystemHighlightColor<T>(this T element)
        where T : FrameworkElement
    {
        element.Resources[SystemColors.HighlightColorKey] = WindowsHighlightColor;
        element.Resources[SystemColors.HighlightBrushKey] = new SolidColorBrush(WindowsHighlightColor);
        return element;
    }
}
