using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace FZScout;

/// <summary>
/// Asks GitHub for a newer release and downloads it, for the caller to mention it has done so.
/// Not being able to check or download is not worth bothering anyone about.
/// </summary>
class GitHubUpdateChecker(IUpdateSource updateSource, IVelopackLocator? locator = null)
{
    /// <summary>Talks to the real GitHub.</summary>
    public GitHubUpdateChecker()
        : this(new GithubSource($"https://github.com/{GitHubRepository.OwnerAndName}", accessToken: null, prerelease: false))
    {
    }

    /// <summary>The downloaded version, which will apply the next time FZScout starts, or null if there was nothing newer to get.</summary>
    public async Task<string?> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var manager = new UpdateManager(updateSource, locator: locator);
            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
            {
                return null;
            }

            await manager.DownloadUpdatesAsync(update, cancelToken: cancellationToken);
            return update.TargetFullRelease.Version.ToString();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
