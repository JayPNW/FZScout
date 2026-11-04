namespace FZScout;

/// <summary>
/// Runs the app in the background behind a system tray icon, starting with the window hidden. Exits when the user chooses Exit,
/// or when the user closes the window after the app has stopped on its own.
/// </summary>
class TrayApplicationContext : ApplicationContext
{
    readonly MainWindow _window;
    readonly NotifyIcon _trayIcon;
    readonly Dictionary<StatusIcon, Icon> _trayIcons = Enum.GetValues<StatusIcon>().ToDictionary(statusIcon => statusIcon, AppIcon.LoadForTray);
    readonly Dictionary<StatusIcon, Icon> _windowIcons = Enum.GetValues<StatusIcon>().ToDictionary(statusIcon => statusIcon, AppIcon.Load);
    readonly AppStatus _status;

    readonly System.Windows.Forms.Timer _showStatusWhenItChangesByItself = new();

    readonly TimeProvider _timeProvider;
    bool _hasExplainedHidingToTray;

    /// <summary>The window isn't the context's main form, because Application.Run would show it.</summary>
    public TrayApplicationContext(MainWindow window, App app)
    {
        this._window = window;
        this._window.FormClosed += (_, _) => ExitThread();
        this._trayIcon = CreateTrayIconWithMenu();
        this._window.HiddenToTray += ExplainHidingToTrayTheFirstTime;
        ShowLogSuccessesAndErrorsInToastsAndStatus(app.Dependencies.Log);
        this._timeProvider = app.Dependencies.TimeProvider;
        this._status = new AppStatus(this._timeProvider);
        ShowStatusNowAndWheneverItChanges(app);
        StartInTrayOrOnTaskbarAndSayWhere();
        RunAppThenStop(app);
    }

    NotifyIcon CreateTrayIconWithMenu()
    {
        var menu = new ContextMenuStrip();
        var showItem = menu.Items.Add("Show FZScout", null, (_, _) => this._window.ShowAndActivate());
        showItem.Font = new Font(showItem.Font, FontStyle.Bold);
        menu.Items.Add("Settings...", null, (_, _) => this._window.ShowSettings());
        menu.Items.Add("Exit", null, (_, _) => this._window.RequestExit());

        var trayIcon = new NotifyIcon
        {
            Icon = this._trayIcons[StatusIcon.NotScouting],
            Text = "FZScout",
            ContextMenuStrip = menu,
            Visible = true,
        };
        trayIcon.DoubleClick += (_, _) => this._window.ShowAndActivate();
        return trayIcon;
    }

    void ShowLogSuccessesAndErrorsInToastsAndStatus(Log log)
    {
        log.SuccessWritten += message => ShowToast(message, ToolTipIcon.Info);
        log.ErrorWritten += message => ShowToast(message, ToolTipIcon.Error);
        log.SuccessWritten += _ => UpdateStatus(status => status.HadProblemSinceLastSuccess = false);
        log.ErrorWritten += _ => UpdateStatus(status => status.HadProblemSinceLastSuccess = true);
    }

    void ShowStatusNowAndWheneverItChanges(App app)
    {
        this._showStatusWhenItChangesByItself.Tick += (_, _) => ShowStatus();
        ShowStatus();
        app.WaitingForBonusMessageUntilChanged += waitingUntil => UpdateStatus(status => status.WaitingForBonusMessageUntil = waitingUntil);
        app.ScoutingStarted += () => UpdateStatus(StartScouting);
    }

    void StartInTrayOrOnTaskbarAndSayWhere()
    {
        var trayIconReachability = new TrayIconReachability();
        this._window.CanUserReachTrayIcon = () => trayIconReachability.CanUserReachTrayIcon(Environment.ProcessPath!);
        this._window.CreateWithoutShowing();
        this._hasExplainedHidingToTray = true;
        if (this._window.CanUserReachTrayIcon())
        {
            this._trayIcon.ShowBalloonTip(5000, "FZScout is running", "Double-click this icon to see the log. To exit, right-click it and choose Exit.", ToolTipIcon.Info);
            return;
        }

        this._window.ShowMinimizedOnTaskbar();
        this._trayIcon.ShowBalloonTip(5000, "FZScout is running", "Windows is hiding its tray icon, so it stays on the taskbar. To show the icon, go to Settings > Personalization > Taskbar > Other system tray icons.", ToolTipIcon.Info);
    }

    protected override void ExitThreadCore()
    {
        this._showStatusWhenItChangesByItself.Dispose();
        this._trayIcon.Visible = false;
        this._trayIcon.Dispose();
        base.ExitThreadCore();
    }

    void ExplainHidingToTrayTheFirstTime()
    {
        if (this._hasExplainedHidingToTray)
        {
            return;
        }

        this._hasExplainedHidingToTray = true;
        this._trayIcon.ShowBalloonTip(5000, "FZScout is still running", "To exit, right-click this icon and choose Exit.", ToolTipIcon.Info);
    }

    /// <summary>Safe to call from any thread.</summary>
    void UpdateStatus(Action<AppStatus> update)
    {
        this._window.BeginInvoke(() =>
        {
            update(this._status);
            ShowStatus();
        });
    }

    /// <summary>Any problems so far were with getting started, like a mistyped API key, and are over now.</summary>
    static void StartScouting(AppStatus status)
    {
        status.IsScouting = true;
        status.HadProblemSinceLastSuccess = false;
    }

    void ShowStatus()
    {
        this._trayIcon.Icon = this._trayIcons[this._status.Icon];
        this._window.Icon = this._windowIcons[this._status.Icon];
        this._trayIcon.Text = this._status.Description;
        this._showStatusWhenItChangesByItself.Stop();
        if (this._status.ChangesByItselfAt is { } changesAt)
        {
            var timeUntilChange = changesAt - this._timeProvider.GetUtcNow();
            this._showStatusWhenItChangesByItself.Interval = Math.Max(1, (int)Math.Ceiling(timeUntilChange.TotalMilliseconds));
            this._showStatusWhenItChangesByItself.Start();
        }
    }

    /// <summary>
    /// Safe to call from any thread. Shows only the first line, so an exception's stack trace doesn't fill the toast.
    /// </summary>
    void ShowToast(string message, ToolTipIcon icon)
    {
        var firstLine = message.Split('\n')[0].TrimEnd('\r');
        this._window.BeginInvoke(() => this._trayIcon.ShowBalloonTip(5000, "FZScout", firstLine, icon));
    }

    /// <summary>Runs off the UI thread, so watching logs never makes the window unresponsive.</summary>
    async void RunAppThenStop(App app)
    {
        try
        {
            await Task.Run(app.RunAsync);
        }
        catch (Exception exception)
        {
            app.Dependencies.Log.Error($"Unexpected problem: {exception}");
        }

        if (this._window.ExitRequested.IsCancellationRequested)
        {
            ExitThread();
            return;
        }

        this._status.HasStopped = true;
        ShowStatus();
        this._window.HidesInsteadOfClosing = false;
        this._window.ShowAndActivate();
    }
}
