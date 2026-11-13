using FZScout.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace FZScout.Tests.Xaml;

public class TrayIconTests
{
    /// <summary>The tray icon itself is drawn by Windows, so this draws its menu.</summary>
    [WpfFact]
    public async Task Menu()
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: true);
        using var trayIcon = new TrayIcon(viewModel);
        var menu = trayIcon.ContextMenu;

        // TaskbarIcon points its menu back at itself with this attached property, and Verify.Xaml's XAML serializer follows that cycle until the stack overflows.
        TaskbarIcon.SetParentTaskbarIcon(menu, null);

        await Verify(menu);
    }
}
