using System.Windows;

namespace FZScout;

/// <summary>A dialog sized to its content that the user can't resize, which opens over its owner without blocking it.</summary>
class FixedSizeDialog : Window
{
    public FixedSizeDialog()
    {
        Icon = WindowIcon.Gem;
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}
