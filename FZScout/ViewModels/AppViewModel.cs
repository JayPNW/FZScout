using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Velopack;

namespace FZScout.ViewModels;

/// <summary>Everything the windows show. Holds no behavior of its own: whoever creates it supplies the commands.</summary>
partial class AppViewModel(
    string? runningVersion,
    string apiKey,
    bool launchesAtLogin,
    bool showsToastNotifications) : ObservableObject
{
    public ObservableCollection<LogLine> LogLines { get; } = [];

    [ObservableProperty]
    public partial ScoutingStatus ScoutingStatus { get; set; } = ScoutingStatus.NotScouting;

    /// <summary>Short enough for a tray icon's tooltip.</summary>
    [ObservableProperty]
    public partial string StatusDescription { get; set; } = "FZScout";

    [ObservableProperty]
    public partial bool ApplicationHasStopped { get; set; }

    public string? RunningVersion { get; } = runningVersion;

    [ObservableProperty]
    public partial bool IsCheckingForUpdates { get; set; }

    [ObservableProperty]
    public partial UpdateCheckResult? LatestUpdateCheckResult { get; set; }

    [ObservableProperty]
    public partial SemanticVersion? DownloadedVersionToInstallAfterExit { get; set; }

    [ObservableProperty]
    public partial string ApiKey { get; set; } = apiKey;

    /// <summary>About <see cref="ApiKey"/> as it is now, so editing it forgets how an earlier key did.</summary>
    [ObservableProperty]
    public partial ApiKeyVerificationStatus ApiKeyVerificationStatus { get; set; } = ApiKeyVerificationStatus.NotVerified;

    [ObservableProperty]
    public partial bool LaunchesAtLogin { get; set; } = launchesAtLogin;

    [ObservableProperty]
    public partial bool ShowsToastNotifications { get; set; } = showsToastNotifications;

    public ICommand ShowLogCommand { get; init; } = new RelayCommand(() => { });

    public ICommand ShowSettingsCommand { get; init; } = new RelayCommand(() => { });

    public ICommand ShowAboutCommand { get; init; } = new RelayCommand(() => { });

    public ICommand VerifyApiKeyCommand { get; init; } = new RelayCommand(() => { });

    public ICommand CheckForUpdatesCommand { get; init; } = new RelayCommand(() => { });

    public ICommand RestartToUpdateCommand { get; init; } = new RelayCommand(() => { });

    public ICommand ExitCommand { get; init; } = new RelayCommand(() => { });

    partial void OnApiKeyChanged(string value) => ApiKeyVerificationStatus = ApiKeyVerificationStatus.NotVerified;
}
