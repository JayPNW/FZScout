using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace FZScout;

/// <summary>
/// Downloads new releases while the app runs, then installs the newest one after the app exits. Checking again while running means
/// a release published after launch gets installed at the next exit, instead of needing another launch to download it first.
/// </summary>
class GitHubUpdateChecker(IUpdateSource updateSource, IVelopackLocator? locator = null)
{
    public static readonly TimeSpan TimeBetweenChecks = TimeSpan.FromDays(1);

    readonly UpdateManager _updateManager = new(updateSource, locator: locator);
    readonly SemaphoreSlim _oneCheckAtATime = new(1, 1);

    public GitHubUpdateChecker()
        : this(CreateGitHubSource(VelopackLocator.Current.CurrentlyInstalledVersion))
    {
    }

    public SemanticVersion? DownloadedVersionToInstallAfterExit => DownloadedUpdateToInstallAfterExit?.TargetFullRelease.Version;

    UpdateInfo? DownloadedUpdateToInstallAfterExit { get; set; }

    /// <summary>Set when the user asks to restart into the update now, so it starts again once installed.</summary>
    public bool RestartAfterInstallingAtExit { get; set; }

    /// <summary>Prerelease builds take prerelease updates, so testers stay on the newest build; stable builds only take stable releases.</summary>
    internal static GithubSource CreateGitHubSource(SemanticVersion? currentlyInstalledVersion)
    {
        var acceptPrereleases = currentlyInstalledVersion?.IsPrerelease == true;
        return new GithubSource($"https://github.com/{GitHubRepository.OwnerAndName}", accessToken: null, prerelease: acceptPrereleases);
    }

    /// <summary>Returns quietly once cancelled, dropping any download still in progress.</summary>
    public async Task DownloadNewestReleaseNowAndDailyUntilCancelled(TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await DownloadNewestRelease(cancellationToken);
                await Task.Delay(TimeBetweenChecks, timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Keeps any earlier download when GitHub is unreachable or has nothing newer. Waits for a check that's already running,
    /// so the daily check and one the user asked for never download at the same time.
    /// </summary>
    public async Task<UpdateCheckResult> DownloadNewestRelease(CancellationToken cancellationToken = default)
    {
        if (!this._updateManager.IsInstalled)
        {
            return new NotInstalledSoCannotUpdate();
        }

        await this._oneCheckAtATime.WaitAsync(cancellationToken);
        try
        {
            return await DownloadNewestReleaseWhileNoOtherCheckIsRunning(cancellationToken);
        }
        finally
        {
            this._oneCheckAtATime.Release();
        }
    }

    async Task<UpdateCheckResult> DownloadNewestReleaseWhileNoOtherCheckIsRunning(CancellationToken cancellationToken)
    {
        try
        {
            var update = await this._updateManager.CheckForUpdatesAsync().WaitAsync(cancellationToken);
            if (update is null)
            {
                return new AlreadyOnNewestRelease();
            }

            await this._updateManager.DownloadUpdatesAsync(update, cancelToken: cancellationToken);
            DownloadedUpdateToInstallAfterExit = update;
            return new DownloadedNewerRelease(update.TargetFullRelease.Version);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new CouldNotCheckForUpdates(exception.Message);
        }
    }

    /// <summary>Call as the app exits; the installer waits for this process to end before replacing it.</summary>
    public void InstallDownloadedUpdateAfterExit()
    {
        if (DownloadedUpdateToInstallAfterExit is { } update)
        {
            this._updateManager.WaitExitThenApplyUpdates(update, restart: RestartAfterInstallingAtExit);
        }
    }
}
