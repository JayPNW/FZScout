namespace FZScout;

/// <summary>
/// Runs the app in the background behind a system tray icon, starting with the window hidden. Exits when the user chooses Exit,
/// or when the user closes the window after the app has stopped on its own.
/// </summary>
class TrayApplicationContext : ApplicationContext
{
    readonly MainWindow _window;
    readonly NotifyIcon _trayIcon;
    bool _hasExplainedHidingToTray;

    /// <summary>The window isn't the context's main form, because Application.Run would show it.</summary>
    public TrayApplicationContext(MainWindow window, App app)
    {
        this._window = window;
        this._window.FormClosed += (_, _) => ExitThread();

        var menu = new ContextMenuStrip();
        var showItem = menu.Items.Add("Show FZScout", null, (_, _) => this._window.ShowAndActivate());
        showItem.Font = new Font(showItem.Font, FontStyle.Bold);
        menu.Items.Add("Exit", null, (_, _) => this._window.RequestExit());

        this._trayIcon = new NotifyIcon
        {
            Icon = AppIcon.LoadForTray(),
            Text = "FZScout",
            ContextMenuStrip = menu,
            Visible = true,
        };
        this._trayIcon.DoubleClick += (_, _) => this._window.ShowAndActivate();
        this._window.HiddenToTray += ExplainHidingToTrayTheFirstTime;
        app.Dependencies.Log.SuccessWritten += message => ShowToast(message, ToolTipIcon.Info);
        app.Dependencies.Log.ErrorWritten += message => ShowToast(message, ToolTipIcon.Error);

        this._window.CreateWithoutShowing();
        this._hasExplainedHidingToTray = true;
        this._trayIcon.ShowBalloonTip(5000, "FZScout is running", "Double-click this icon to see the log. To exit, right-click it and choose Exit.", ToolTipIcon.Info);
        RunAppThenStop(app);
    }

    protected override void ExitThreadCore()
    {
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

        this._window.HidesInsteadOfClosing = false;
        this._window.ShowAndActivate();
    }
}
