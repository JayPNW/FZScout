using FZScout.ViewModels;
using Velopack;
using static FZScout.Tests.Xaml.ScreenshotVerifier;

namespace FZScout.Tests.Xaml;

public class AboutWindowTests
{
    [Fact]
    public Task AfterDownloadingAnUpdate() => VerifyScreenshot(() =>
    {
        var downloadedVersion = SemanticVersion.Parse("8.0.0");
        var viewModel = new AppViewModel("7.0.0", apiKey: "", launchesAtLogin: true, showsToastNotifications: true)
        {
            LatestUpdateCheckResult = new DownloadedNewerRelease(downloadedVersion),
            DownloadedVersionToInstallAfterExit = downloadedVersion,
        };
        return new AboutWindow(viewModel);
    });
}
