using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace FZScout;

class GitHubUpdateChecker(IUpdateSource updateSource, IVelopackLocator? locator = null)
{
    public GitHubUpdateChecker()
        : this(new GithubSource($"https://github.com/{GitHubRepository.OwnerAndName}", accessToken: null, prerelease: false))
    {
    }

    /// <summary>Downloads a newer release if one exists, returning something that applies it once disposed, or null if there was nothing newer.</summary>
    public async Task<IDisposable?> DownloadLatestAndApplyOnDispose(CancellationToken cancellationToken = default)
    {
        try
        {
            var updateManager = new UpdateManager(updateSource, locator: locator);
            return await updateManager.DownloadLatestAndApplyOnDispose(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
