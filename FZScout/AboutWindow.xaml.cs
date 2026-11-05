using System.Windows;
using FZScout.ViewModels;

namespace FZScout;

/// <summary>
/// Shows which version is running. Checks for a newer release every time it opens, downloading it to install when FZScout exits.
/// Once one is downloaded, offers to restart into it right away.
/// </summary>
partial class AboutWindow : FixedSizeDialog
{
    public AboutWindow(AppViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.CheckForUpdatesCommand.Execute(null);
    }

    void CloseWindow(object sender, RoutedEventArgs e) => Close();
}
