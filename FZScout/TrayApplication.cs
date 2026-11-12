using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using FZScout.Site;
using FZScout.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace FZScout;

/// <summary>
/// Runs the app in the background behind a system tray icon, starting with the window hidden. Shuts WPF down when the user chooses Exit,
/// or when the user closes the window after the app has stopped on its own. Gives the windows' <see cref="AppViewModel"/> its behavior.
/// </summary>
class TrayApplication : IDisposable
{
    readonly App _app;
    readonly AppDependencies _dependencies;
    readonly MessageBoxUserInput _userInput;
    readonly GitHubUpdateChecker _updateChecker;
    readonly Dispatcher _uiThread = Dispatcher.CurrentDispatcher;
    readonly AppViewModel _viewModel;
    readonly MainWindow _window;
    readonly TrayIcon _trayIcon;
    readonly Dictionary<ScoutingStatus, Icon> _trayIcons = Enum.GetValues<ScoutingStatus>().ToDictionary(scoutingStatus => scoutingStatus, AppIcon.LoadForTray);
    readonly AppStatus _status;
    readonly DispatcherTimer _showStatusWhenItChangesByItself = new();
    readonly LaunchAtLogin _launchAtLogin = new();
    readonly ShowToastNotifications _showToastNotifications = new();
    readonly ApiKeyVerifier _apiKeyVerifier;
    readonly AsyncRelayCommand _verifyApiKeyCommand;
    bool _hasExplainedHidingToTray;
    SettingsWindow? _openSettingsWindow;
    AboutWindow? _openAboutWindow;

    /// <summary>Call on the UI thread, before WPF starts running. Nothing happens until <see cref="Start"/>.</summary>
    public TrayApplication(App app, MessageBoxUserInput userInput, GitHubUpdateChecker updateChecker)
    {
        this._app = app;
        this._dependencies = app.Dependencies;
        this._userInput = userInput;
        this._updateChecker = updateChecker;
        this._status = new AppStatus(this._dependencies.TimeProvider);
        this._apiKeyVerifier = new ApiKeyVerifier(this._dependencies.ApiKeyStore, this._dependencies.FangbreakerZoneClient, this._dependencies.Log);
        this._apiKeyVerifier.ApiKeySaved += () => UpdateStatus(status => status.HadProblemSinceLastSuccess = false);
        this._verifyApiKeyCommand = new AsyncRelayCommand(VerifyThenSaveApiKeyAsync, CanVerifyApiKey);
        this._viewModel = CreateViewModel();
        this._viewModel.PropertyChanged += (_, e) => ApplyChangedSetting(e);
        ShowLogLinesInWindow(this._dependencies.Log);
        this._window = new MainWindow(this._viewModel);
        this._window.Closed += (_, _) => Application.Current.Shutdown();
        this._window.HiddenToTray += ExplainHidingToTrayTheFirstTime;
        this._trayIcon = new TrayIcon(this._viewModel);
        ShowLogSuccessesAndErrorsInToastsAndStatus(this._dependencies.Log);
        this._showStatusWhenItChangesByItself.Tick += (_, _) => ShowStatus();
        ShowStatus();
        app.WaitingForBonusMessageUntilChanged += waitingUntil => UpdateStatus(status => status.WaitingForBonusMessageUntil = waitingUntil);
        app.ScoutingStarted += () => UpdateStatus(status => status.IsScouting = true);
    }

    string ApiKeyBeingEdited => this._viewModel.ApiKeyBeingEdited.Trim();

    /// <summary>Call once WPF is running, so the app's work can come back to the UI thread.</summary>
    public void Start()
    {
        StartInTrayOrOnTaskbarAndSayWhere();
        ShowSettingsIfThereIsNoApiKey();
        RunAppThenStop();
    }

    public void Dispose()
    {
        this._showStatusWhenItChangesByItself.Stop();
        this._trayIcon.Dispose();
        foreach (var icon in this._trayIcons.Values)
        {
            icon.Dispose();
        }
    }

    AppViewModel CreateViewModel()
    {
        return new AppViewModel(this._dependencies.GetVersion(), this._launchAtLogin.IsOn, this._showToastNotifications.IsOn)
        {
            DownloadedVersionToInstallAfterExit = this._updateChecker.DownloadedVersionToInstallAfterExit,
            ShowLogCommand = new RelayCommand(ShowLog),
            ShowSettingsCommand = new RelayCommand(ShowSettings),
            ShowAboutCommand = new RelayCommand(ShowAbout),
            VerifyApiKeyCommand = this._verifyApiKeyCommand,
            CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync),
            RestartToUpdateCommand = new RelayCommand(ExitToInstallUpdateThenRestart),
            ExitCommand = new RelayCommand(Exit),
        };
    }

    /// <summary>Safe to write from any thread, since each line is added on the UI thread.</summary>
    void ShowLogLinesInWindow(Log log)
    {
        log.LineWritten += (line, severity) => this._uiThread.BeginInvoke(() => this._viewModel.LogLines.Add(new LogLine(line, severity)));
    }

    void ShowLogSuccessesAndErrorsInToastsAndStatus(Log log)
    {
        log.SuccessWritten += message => ShowToast(message, BalloonIcon.Info);
        log.ErrorWritten += message => ShowToast(message, BalloonIcon.Error);
        log.SuccessWritten += _ => UpdateStatus(status => status.HadProblemSinceLastSuccess = false);
        log.ErrorWritten += _ => UpdateStatus(status => status.HadProblemSinceLastSuccess = true);
    }

    void StartInTrayOrOnTaskbarAndSayWhere()
    {
        var trayIconReachability = new TrayIconReachability();
        this._window.CanUserReachTrayIcon = () => trayIconReachability.CanUserReachTrayIcon(Environment.ProcessPath!);
        this._hasExplainedHidingToTray = true;
        if (this._window.CanUserReachTrayIcon())
        {
            ShowToastIfTurnedOn("FZScout is running", "Double-click this icon to see the log. To exit, right-click it and choose Exit.", BalloonIcon.Info);
            return;
        }

        this._window.ShowMinimizedOnTaskbar();
        ShowToastIfTurnedOn("FZScout is running", "Windows is hiding its tray icon, so it stays on the taskbar. To show the icon, go to Settings > Personalization > Taskbar > Other system tray icons.", BalloonIcon.Info);
    }

    void ShowSettingsIfThereIsNoApiKey()
    {
        if (this._dependencies.ApiKeyStore.Load() is null)
        {
            ShowSettings();
        }
    }

    /// <summary>The window is created after the view model, so this looks it up when the command runs.</summary>
    void ShowLog() => this._window.ShowAndActivate();

    /// <summary>
    /// Brings an already open Settings window to the front, instead of opening a second one. A new one starts with an empty API key box
    /// rather than one typed earlier that was never saved, and doesn't show how an earlier Verify went.
    /// </summary>
    void ShowSettings()
    {
        this._window.ShowAndActivate();
        if (this._openSettingsWindow is not null)
        {
            this._openSettingsWindow.Activate();
            return;
        }

        this._viewModel.SavedApiKeyAbbreviation = this._apiKeyVerifier.SavedApiKey?.AbbreviatedForDisplay ?? "";
        this._viewModel.ApiKeyBeingEdited = "";
        this._viewModel.ApiKeyVerificationStatus = ApiKeyVerificationStatus.NotVerified;
        var settings = new SettingsWindow(this._viewModel) { Owner = this._window };
        settings.Closed += (_, _) => this._openSettingsWindow = null;
        this._openSettingsWindow = settings;
        settings.Show();
    }

    /// <summary>Brings an already open About window to the front, instead of opening a second one.</summary>
    void ShowAbout()
    {
        this._window.ShowAndActivate();
        if (this._openAboutWindow is not null)
        {
            this._openAboutWindow.Activate();
            return;
        }

        var about = new AboutWindow(this._viewModel) { Owner = this._window };
        about.Closed += (_, _) => this._openAboutWindow = null;
        this._openAboutWindow = about;
        about.Show();
    }

    void ApplyChangedSetting(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppViewModel.ApiKeyBeingEdited))
        {
            this._verifyApiKeyCommand.NotifyCanExecuteChanged();
        }

        if (e.PropertyName == nameof(AppViewModel.LaunchesAtLogin))
        {
            ApplyLaunchAtLogin(this._viewModel.LaunchesAtLogin);
        }

        if (e.PropertyName == nameof(AppViewModel.ShowsToastNotifications))
        {
            ApplyShowToastNotifications(this._viewModel.ShowsToastNotifications);
        }
    }

    void ApplyLaunchAtLogin(bool wantsLaunchAtLogin)
    {
        if (wantsLaunchAtLogin == this._launchAtLogin.IsOn)
        {
            return;
        }

        if (wantsLaunchAtLogin)
        {
            this._launchAtLogin.TurnOn(Environment.ProcessPath!);
            return;
        }

        this._launchAtLogin.TurnOff();
    }

    void ApplyShowToastNotifications(bool wantsToastNotifications)
    {
        if (wantsToastNotifications)
        {
            this._showToastNotifications.TurnOn();
            return;
        }

        this._showToastNotifications.TurnOff();
    }

    bool CanVerifyApiKey() => ApiKeyBeingEdited.Length > 0 && ApiKeyBeingEdited != this._apiKeyVerifier.SavedApiKey?.Value;

    async Task VerifyThenSaveApiKeyAsync()
    {
        this._viewModel.ApiKeyVerificationStatus = ApiKeyVerificationStatus.Verifying;
        this._viewModel.ApiKeyVerificationStatus = await this._apiKeyVerifier.VerifyThenSaveAsync(new ApiKey(ApiKeyBeingEdited));
    }

    /// <summary>Lets the download finish even if the About window closes first, so it still gets installed when FZScout exits.</summary>
    async Task CheckForUpdatesAsync()
    {
        this._viewModel.IsCheckingForUpdates = true;
        this._viewModel.LatestUpdateCheckResult = await this._updateChecker.DownloadNewestRelease();
        this._viewModel.DownloadedVersionToInstallAfterExit = this._updateChecker.DownloadedVersionToInstallAfterExit;
        this._viewModel.IsCheckingForUpdates = false;
    }

    void ExitToInstallUpdateThenRestart()
    {
        this._updateChecker.RestartAfterInstallingAtExit = true;
        Exit();
    }

    /// <summary>
    /// Asks the app to shut down cleanly, and shuts WPF down once it has. If the app has already stopped on its own, nothing is left
    /// to notice the exit request, so this shuts WPF down directly.
    /// </summary>
    void Exit()
    {
        this._userInput.RequestExit();
        if (this._status.HasStopped)
        {
            ShutDown();
        }
    }

    /// <summary>Closing the window shuts WPF down; shutting down too covers a window that was never shown.</summary>
    void ShutDown()
    {
        this._window.CloseToExit();
        Application.Current.Shutdown();
    }

    void ExplainHidingToTrayTheFirstTime()
    {
        if (this._hasExplainedHidingToTray)
        {
            return;
        }

        this._hasExplainedHidingToTray = true;
        ShowToastIfTurnedOn("FZScout is still running", "To exit, right-click this icon and choose Exit.", BalloonIcon.Info);
    }

    /// <summary>Safe to call from any thread.</summary>
    void UpdateStatus(Action<AppStatus> update)
    {
        this._uiThread.BeginInvoke(() =>
        {
            update(this._status);
            ShowStatus();
        });
    }

    void ShowStatus()
    {
        this._viewModel.ScoutingStatus = this._status.ScoutingStatus;
        this._viewModel.ApplicationHasStopped = this._status.HasStopped;
        this._trayIcon.Icon = this._trayIcons[this._status.ScoutingStatus];
        this._viewModel.StatusDescription = this._status.Description;
        this._showStatusWhenItChangesByItself.Stop();
        if (this._status.ChangesByItselfAt is { } changesAt)
        {
            var timeUntilChange = changesAt - this._dependencies.TimeProvider.GetUtcNow();
            this._showStatusWhenItChangesByItself.Interval = TimeSpan.FromMilliseconds(Math.Max(1, Math.Ceiling(timeUntilChange.TotalMilliseconds)));
            this._showStatusWhenItChangesByItself.Start();
        }
    }

    /// <summary>
    /// Safe to call from any thread. Shows only the first line, so an exception's stack trace doesn't fill the toast.
    /// </summary>
    void ShowToast(string message, BalloonIcon icon)
    {
        var firstLine = message.Split('\n')[0].TrimEnd('\r');
        this._uiThread.BeginInvoke(() => ShowToastIfTurnedOn("FZScout", firstLine, icon));
    }

    void ShowToastIfTurnedOn(string title, string text, BalloonIcon icon)
    {
        if (!this._viewModel.ShowsToastNotifications)
        {
            return;
        }

        this._trayIcon.ShowBalloonTip(title, text, icon);
    }

    /// <summary>Runs off the UI thread, so watching logs never makes the window unresponsive.</summary>
    async void RunAppThenStop()
    {
        try
        {
            await Task.Run(this._app.RunAsync);
        }
        catch (Exception exception)
        {
            this._dependencies.Log.Error($"Unexpected problem: {exception}");
        }

        if (this._userInput.ExitRequested.IsCancellationRequested)
        {
            ShutDown();
            return;
        }

        this._status.HasStopped = true;
        ShowStatus();
        this._window.ShowAndActivate();
    }
}
