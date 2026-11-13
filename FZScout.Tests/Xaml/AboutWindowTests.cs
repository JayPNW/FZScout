using FZScout.ViewModels;
using Velopack;

namespace FZScout.Tests.Xaml;

public class AboutWindowTests
{
    [WpfFact]
    public Task AfterDownloadingAnUpdate()
    {
        var downloadedVersion = SemanticVersion.Parse("8.0.0");
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: true)
        {
            LatestUpdateCheckResult = new DownloadedNewerRelease(downloadedVersion),
            DownloadedVersionToInstallAfterExit = downloadedVersion,
        };
        return Verify(new AboutWindow(viewModel));
    }
}
