using FZScout.ViewModels;

namespace FZScout.Tests.Xaml;

public class SettingsWindowTests
{
    [WpfFact]
    public Task AfterVerifyingTheApiKey()
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: false)
        {
            ApiKeyBeingEdited = "fz_live_not_a_real_key",
            ApiKeyVerificationStatus = ApiKeyVerificationStatus.VerifiedAndSaved,
        };
        return Verify(new SettingsWindow(viewModel));
    }

    [WpfFact]
    public Task ShowsWhichApiKeyIsSavedWithoutRevealingIt()
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: false)
        {
            SavedApiKeyAbbreviation = "fz_live_qa81...cUMfgC",
        };
        return Verify(new SettingsWindow(viewModel));
    }
}
