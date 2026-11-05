using FZScout.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace FZScout;

/// <summary>
/// FZScout's icon in the system tray, with its menu. It isn't in any window, so it doesn't inherit a DataContext.
/// Its <see cref="TaskbarIcon.Icon"/> can't be bound, so whoever creates it sets that.
/// </summary>
partial class TrayIcon : TaskbarIcon
{
    public TrayIcon(AppViewModel viewModel)
    {
        // Before InitializeComponent, so the menu gets the view model as soon as it's created.
        // Otherwise TaskbarIcon gives the menu the TrayIcon itself, and its bindings fail until DataContext is set.
        DataContext = viewModel;
        InitializeComponent();
    }
}
