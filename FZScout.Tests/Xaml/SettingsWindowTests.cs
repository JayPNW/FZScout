using FZScout.ViewModels;
using static FZScout.Tests.Xaml.ScreenshotVerifier;

namespace FZScout.Tests.Xaml;

public class SettingsWindowTests
{
    [Fact]
    public Task AfterVerifyingTheApiKey() => VerifyScreenshot(() =>
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: false)
        {
            ApiKeyBeingEdited = "fz_live_not_a_real_key",
            ApiKeyVerificationStatus = ApiKeyVerificationStatus.VerifiedAndSaved,
        };
        return new SettingsWindow(viewModel);
    });

    [Fact]
    public Task ShowsWhichApiKeyIsSavedWithoutRevealingIt() => VerifyScreenshot(() =>
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: false)
        {
            SavedApiKeyAbbreviation = "fz_live_qa81...cUMfgC",
        };
        return new SettingsWindow(viewModel);
    });
}
