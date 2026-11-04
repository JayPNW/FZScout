using System.ComponentModel;
using FZScout.Site;

namespace FZScout;

/// <summary>
/// Shows the log, and opens Settings when the user asks or the app needs an API key. Safe to call from any thread once created, except where noted.
/// </summary>
class MainWindow : Form, IUserInput
{
    readonly CancellationTokenSource _exitRequested = new();
    readonly RichTextBox _logView;
    SettingsWindow? _openSettingsWindow;

    public MainWindow()
    {
        Text = "FZScout";
        Icon = AppIcon.Load();
        ClientSize = new Size(900, 450);
        StartPosition = FormStartPosition.CenterScreen;

        this._logView = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.Gainsboro,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None,
            DetectUrls = true,
            WordWrap = true,
        };
        this._logView.LinkClicked += (_, e) => Browser.Open(e.LinkText!);

        Controls.Add(this._logView);
    }

    /// <summary>Raised when the user minimizes the window, or closes it while the app is still scouting, and it hides to the system tray. Not raised when it minimizes to the taskbar instead.</summary>
    public event Action? HiddenToTray;

    public CancellationToken ExitRequested => this._exitRequested.Token;

    /// <summary>While true, closing the window just hides it, so the app keeps scouting from the system tray.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HidesInsteadOfClosing { get; set; } = true;

    /// <summary>When the user can't reach the tray icon, hiding the window would lose it, so it minimizes to the taskbar instead.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<bool> CanUserReachTrayIcon { get; set; } = () => true;

    public void RequestExit() => this._exitRequested.Cancel();

    /// <summary>Once the user has seen it there is nothing left to do, so this also asks the app to exit.</summary>
    public void InformUserAppIsAlreadyRunning()
    {
        MessageBox.Show("FZScout is already running. Look for its icon in the system tray.", "FZScout", MessageBoxButtons.OK, MessageBoxIcon.Information);
        RequestExit();
    }

    public Task<ApiKey?> ReadApiKeyAsync(CancellationToken cancellationToken)
    {
        var answer = new TaskCompletionSource<ApiKey?>(TaskCreationOptions.RunContinuationsAsynchronously);
        BeginInvoke(() => AskForApiKeyInSettings(answer));
        return answer.Task.WaitAsync(cancellationToken);
    }

    public void WriteLine(string line, ConsoleColor? color) => BeginInvoke(() => AppendLine(line, color));

    /// <summary>Lets other threads write to the window and ask for the API key before the user ever opens it.</summary>
    public void CreateWithoutShowing() => _ = Handle;

    /// <summary>Brings an already open Settings window to the front, instead of opening a second one. Call on the UI thread.</summary>
    public void ShowSettings()
    {
        ShowAndActivate();
        if (this._openSettingsWindow is not null)
        {
            this._openSettingsWindow.Activate();
            return;
        }

        OpenSettings(asksForApiKey: false, onClosed: _ => { });
    }

    public void ShowMinimizedOnTaskbar()
    {
        WindowState = FormWindowState.Minimized;
        Show();
    }

    public void ShowAndActivate()
    {
        Show();
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && HidesInsteadOfClosing)
        {
            e.Cancel = true;
            HideToTrayOrMinimize();
        }

        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized && Visible && CanUserReachTrayIcon())
        {
            HideToTray();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._exitRequested.Dispose();
        }

        base.Dispose(disposing);
    }

    static Color ToTextColor(ConsoleColor? color)
    {
        if (color is null)
        {
            return Color.Gainsboro;
        }

        return Color.FromName(color.Value.ToString());
    }

    /// <summary>Replaces a Settings window the user already opened, since that one has no API key box.</summary>
    void AskForApiKeyInSettings(TaskCompletionSource<ApiKey?> answer)
    {
        ShowAndActivate();
        this._openSettingsWindow?.Close();
        OpenSettings(asksForApiKey: true, onClosed: settings => answer.TrySetResult(settings.EnteredApiKey));
    }

    void OpenSettings(bool asksForApiKey, Action<SettingsWindow> onClosed)
    {
        var settings = new SettingsWindow(new LaunchAtLogin(), asksForApiKey);
        this._openSettingsWindow = settings;
        settings.FormClosed += (_, _) =>
        {
            if (this._openSettingsWindow == settings)
            {
                this._openSettingsWindow = null;
            }

            onClosed(settings);
            settings.Dispose();
        };
        settings.ShowCenteredOver(this);
    }

    void HideToTrayOrMinimize()
    {
        if (CanUserReachTrayIcon())
        {
            HideToTray();
            return;
        }

        WindowState = FormWindowState.Minimized;
    }

    void HideToTray()
    {
        Hide();
        HiddenToTray?.Invoke();
    }

    void AppendLine(string line, ConsoleColor? color)
    {
        this._logView.SelectionStart = this._logView.TextLength;
        this._logView.SelectionLength = 0;
        this._logView.SelectionColor = ToTextColor(color);
        this._logView.AppendText(line + Environment.NewLine);
        this._logView.ScrollToCaret();
    }
}
