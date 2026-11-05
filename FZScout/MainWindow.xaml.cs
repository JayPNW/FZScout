using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using FZScout.ViewModels;

namespace FZScout;

/// <summary>Shows the log, with links to click.</summary>
partial class MainWindow : Window
{
    readonly AppViewModel _viewModel;

    public MainWindow(AppViewModel viewModel)
    {
        InitializeComponent();
        this._viewModel = viewModel;
        DataContext = viewModel;
        AppendLines(viewModel.LogLines);
        viewModel.LogLines.CollectionChanged += (_, e) => AppendLines(e.NewItems?.Cast<LogLine>() ?? []);
    }

    /// <summary>Raised when the user minimizes the window, or closes it while the app is still scouting, and it hides to the system tray. Not raised when it minimizes to the taskbar instead.</summary>
    public event Action? HiddenToTray;

    /// <summary>When the user can't reach the tray icon, hiding the window would lose it, so it minimizes to the taskbar instead.</summary>
    public Func<bool> CanUserReachTrayIcon { get; set; } = () => true;

    bool IsClosingToExit { get; set; }

    bool HidesInsteadOfClosing => !this._viewModel.ApplicationHasStopped && !IsClosingToExit;

    public void ShowMinimizedOnTaskbar()
    {
        WindowState = WindowState.Minimized;
        Show();
    }

    public void ShowAndActivate()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    public void CloseToExit()
    {
        IsClosingToExit = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (HidesInsteadOfClosing)
        {
            e.Cancel = true;
            HideToTrayOrMinimize();
        }

        base.OnClosing(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized && IsVisible && CanUserReachTrayIcon())
        {
            HideToTray();
        }
    }

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex UrlPattern();

    static Brush ToTextColor(LogLineSeverity severity) => severity switch
    {
        LogLineSeverity.Info => Brushes.Gainsboro,
        LogLineSeverity.Success => Brushes.Cyan,
        LogLineSeverity.Warning => Brushes.Yellow,
        LogLineSeverity.Error => Brushes.Red,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
    };

    /// <summary>Splits the text so each URL in it is a link.</summary>
    static IEnumerable<Inline> TextWithClickableUrls(string text)
    {
        var textAfterLastUrl = 0;
        foreach (Match url in UrlPattern().Matches(text))
        {
            yield return new Run(text[textAfterLastUrl..url.Index]);
            yield return CreateLinkThatOpensInBrowser(url.Value);
            textAfterLastUrl = url.Index + url.Length;
        }

        yield return new Run(text[textAfterLastUrl..]);
    }

    static Hyperlink CreateLinkThatOpensInBrowser(string url)
    {
        var link = new Hyperlink(new Run(url)) { Foreground = Brushes.DeepSkyBlue };
        link.Click += (_, _) => Browser.Open(url);
        return link;
    }

    void HideToTrayOrMinimize()
    {
        if (CanUserReachTrayIcon())
        {
            HideToTray();
            return;
        }

        WindowState = WindowState.Minimized;
    }

    void HideToTray()
    {
        Hide();
        HiddenToTray?.Invoke();
    }

    void AppendLines(IEnumerable<LogLine> lines)
    {
        foreach (var line in lines)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0), Foreground = ToTextColor(line.Severity) };
            paragraph.Inlines.AddRange(TextWithClickableUrls(line.Text));
            this.LogDocument.Blocks.Add(paragraph);
        }

        this.LogView.ScrollToEnd();
    }
}
