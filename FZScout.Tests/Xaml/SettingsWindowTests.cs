using FZScout.ViewModels;

namespace FZScout.Tests.Xaml;

public class SettingsWindowTests
{
    [WpfFact]
    public Task AfterVerifyingTheApiKey()
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: false, downloadsUpdatesAutomatically: true)
        {
            ApiKeyBeingEdited = "fz_live_not_a_real_key",
            ApiKeyVerificationStatus = ApiKeyVerificationStatus.VerifiedAndSaved,
        };
        return Verify(new SettingsWindow(viewModel).WithPinnedSystemHighlightColor());
    }

    [WpfFact]
    public Task ShowsWhichApiKeyIsSavedWithoutRevealingIt()
    {
        var viewModel = new AppViewModel("7.0.0", launchesAtLogin: true, showsToastNotifications: false, downloadsUpdatesAutomatically: true)
        {
            SavedApiKeyAbbreviation = "fz_live_qa81...cUMfgC",
        };
        return Verify(new SettingsWindow(viewModel).WithPinnedSystemHighlightColor());
    }
}
