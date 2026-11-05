using FZScout.ViewModels;
using static FZScout.Tests.Xaml.ScreenshotVerifier;

namespace FZScout.Tests.Xaml;

public class SettingsWindowTests
{
    [Fact]
    public Task AfterVerifyingTheApiKey() => VerifyScreenshot(() =>
    {
        var viewModel = new AppViewModel("7.0.0", apiKey: "fz_live_not_a_real_key", launchesAtLogin: true, showsToastNotifications: false)
        {
            ApiKeyVerificationStatus = ApiKeyVerificationStatus.VerifiedAndSaved,
        };
        return new SettingsWindow(viewModel);
    });
}
