using System.Diagnostics;

namespace FZScout;

/// <summary>
/// Shows which version is running. Checks for a newer release every time it opens, downloading it to install when FZScout exits.
/// Once one is downloaded, offers to restart into it right away.
/// </summary>
class AboutWindow : FixedSizeDialog
{
    const string ReleasesUrl = $"https://github.com/{GitHubRepository.OwnerAndName}/releases";

    readonly GitHubUpdateChecker _updateChecker;
    readonly Action _exitToInstallUpdateThenRestart;
    readonly Label _updateCheckResult;
    readonly Button _restartToUpdateButton;

    public AboutWindow(string? runningVersion, GitHubUpdateChecker updateChecker, Action exitToInstallUpdateThenRestart)
    {
        this._updateChecker = updateChecker;
        this._exitToInstallUpdateThenRestart = exitToInstallUpdateThenRestart;
        this._restartToUpdateButton = new Button { Text = "Restart to update", AutoSize = true, Visible = false };
        this._restartToUpdateButton.Click += (_, _) => RestartToUpdate();
        Text = "About FZScout";

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Padding = new Padding(12) };
        var appName = new Label { Text = "FZScout", AutoSize = true, Font = new Font(Font.FontFamily, Font.Size * 1.5f, FontStyle.Bold) };
        var version = new Label { Text = $"Version {runningVersion ?? "unknown"}", AutoSize = true };
        this._updateCheckResult = new Label { Text = "Checking for updates...", AutoSize = true, MaximumSize = new Size(360, 0), Margin = new Padding(3, 12, 3, 3) };
        var releases = new LinkLabel { Text = "See all releases", AutoSize = true };
        releases.LinkClicked += (_, _) => Browser.Open(ReleasesUrl);
        layout.Controls.Add(appName);
        layout.Controls.Add(version);
        layout.Controls.Add(this._updateCheckResult);
        layout.Controls.Add(releases);
        layout.Controls.Add(CreateCloseButtonRow());
        Controls.Add(layout);

        Shown += async (_, _) => await CheckForUpdatesAndShowResultAsync();
    }

    Control CreateCloseButtonRow()
    {
        var closeButton = new Button { Text = "Close", AutoSize = true };
        closeButton.Click += (_, _) => Close();
        AcceptButton = closeButton;
        CancelButton = closeButton;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(closeButton);
        buttons.Controls.Add(this._restartToUpdateButton);
        return buttons;
    }

    void RestartToUpdate()
    {
        Close();
        this._exitToInstallUpdateThenRestart();
    }

    /// <summary>Lets the download finish even if the window closes first, so it still gets installed when FZScout exits.</summary>
    async Task CheckForUpdatesAndShowResultAsync()
    {
        var result = await this._updateChecker.DownloadNewestRelease();
        if (IsDisposed)
        {
            return;
        }

        this._updateCheckResult.Text = Describe(result);
        this._restartToUpdateButton.Visible = this._updateChecker.DownloadedVersionToInstallAfterExit is not null;
    }

    string Describe(UpdateCheckResult result) => result switch
    {
        AlreadyOnNewestRelease => "FZScout is up to date.",
        DownloadedNewerRelease downloaded => DescribeDownloadedVersion(downloaded.Version),
        NotInstalledSoCannotUpdate => "This copy of FZScout isn't installed, so it can't update itself.",
        CouldNotCheckForUpdates couldNotCheck => DescribeProblemCheckingForUpdates(couldNotCheck.Problem),
        _ => throw new UnreachableException(),
    };

    static string DescribeDownloadedVersion(Velopack.SemanticVersion version) => $"Version {version} is downloaded, and will be installed when FZScout exits.";

    /// <summary>Mentions an update an earlier check downloaded, since it still gets installed.</summary>
    string DescribeProblemCheckingForUpdates(string problem)
    {
        var couldNotCheck = $"Couldn't check for updates: {problem}";
        if (this._updateChecker.DownloadedVersionToInstallAfterExit is not { } earlierDownload)
        {
            return couldNotCheck;
        }

        return $"{couldNotCheck}{Environment.NewLine}{Environment.NewLine}{DescribeDownloadedVersion(earlierDownload)}";
    }
}
