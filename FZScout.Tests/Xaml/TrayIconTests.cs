using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FZScout.ViewModels;

namespace FZScout.Tests.Xaml;

public class TrayIconTests
{
    /// <summary>The tray icon itself is drawn by Windows, so this draws its menu.</summary>
    [WpfFact]
    public async Task Menu()
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: true);
        using var trayIcon = new TrayIcon(viewModel);
        var png = TakePngOfOpenMenu(trayIcon.ContextMenu);
        await Verify(png, "png");
    }

    /// <summary>Verify.Xaml can't draw a ContextMenu, because it puts what it draws in a window, and a ContextMenu can't have a parent.</summary>
    static MemoryStream TakePngOfOpenMenu(ContextMenu menu)
    {
        menu.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        menu.Arrange(new Rect(menu.DesiredSize));
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(menu.ActualWidth), (int)Math.Ceiling(menu.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(menu);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var png = new MemoryStream();
        encoder.Save(png);
        png.Position = 0;
        return png;
    }
}
