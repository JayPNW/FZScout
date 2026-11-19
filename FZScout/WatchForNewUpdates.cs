namespace FZScout;

/// <summary>
/// Downloads updates in the background from construction until disposed, while the user has automatic updates turned on, then installs the newest one after the process exits.
/// Disposing drops any download still in progress, so exiting never waits on the network.
/// </summary>
class WatchForNewUpdates : IDisposable
{
    readonly CancellationTokenSource _stopCheckingForUpdates = new();
    readonly Task _checkingForUpdates;

    public WatchForNewUpdates(Func<bool> isTurnedOn, TimeProvider timeProvider)
    {
        this._checkingForUpdates = Task.Run(() => UpdateChecker.DownloadNewestReleaseNowAndPeriodicallyUntilCancelled(isTurnedOn, timeProvider, this._stopCheckingForUpdates.Token));
    }

    /// <summary>For checking right away, on top of the periodic checks. Whatever it downloads also gets installed after exit.</summary>
    public GitHubUpdateChecker UpdateChecker { get; } = new();

    public void Dispose()
    {
        this._stopCheckingForUpdates.Cancel();
        this._checkingForUpdates.GetAwaiter().GetResult();
        this._stopCheckingForUpdates.Dispose();
        UpdateChecker.InstallDownloadedUpdateAfterExit();
    }
}
