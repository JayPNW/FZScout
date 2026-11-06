using FZScout.ViewModels;
using static FZScout.Tests.Xaml.ScreenshotVerifier;

namespace FZScout.Tests.Xaml;

public class TrayIconTests
{
    /// <summary>The tray icon itself is drawn by Windows, so this draws its menu.</summary>
    [Fact]
    public Task Menu() => VerifyPng(() =>
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: true);
        using var trayIcon = new TrayIcon(viewModel);
        return Screenshot.TakePng(trayIcon.ContextMenu);
    });
}
