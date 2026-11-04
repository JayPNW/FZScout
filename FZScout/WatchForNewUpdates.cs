namespace FZScout;

/// <summary>
/// Downloads updates in the background from construction until disposed, then installs the newest one after the process exits.
/// Disposing drops any download still in progress, so exiting never waits on the network.
/// </summary>
class WatchForNewUpdates : IDisposable
{
    readonly GitHubUpdateChecker _updateChecker = new();
    readonly CancellationTokenSource _stopCheckingForUpdates = new();
    readonly Task _checkingForUpdates;

    public WatchForNewUpdates(TimeProvider timeProvider)
    {
        this._checkingForUpdates = Task.Run(() => this._updateChecker.DownloadNewestReleaseNowAndDailyUntilCancelled(timeProvider, this._stopCheckingForUpdates.Token));
    }

    public void Dispose()
    {
        this._stopCheckingForUpdates.Cancel();
        this._checkingForUpdates.GetAwaiter().GetResult();
        this._stopCheckingForUpdates.Dispose();
        this._updateChecker.InstallDownloadedUpdateAfterExit();
    }
}
