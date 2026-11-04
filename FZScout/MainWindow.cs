using System.ComponentModel;
using FZScout.Site;

namespace FZScout;

/// <summary>
/// Shows the log, and opens Settings when the app needs an API key. Safe to call from any thread once created.
/// </summary>
class MainWindow : Form, IUserInput
{
    readonly CancellationTokenSource _exitRequested = new();
    readonly RichTextBox _logView;

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

    void AskForApiKeyInSettings(TaskCompletionSource<ApiKey?> answer)
    {
        ShowAndActivate();
        var settings = new SettingsWindow();
        settings.FormClosed += (_, _) =>
        {
            answer.TrySetResult(settings.EnteredApiKey);
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
