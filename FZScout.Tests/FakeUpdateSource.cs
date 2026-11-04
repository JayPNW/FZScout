using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace FZScout.Tests;

/// <summary>Stands in for GitHub: answers a release check with one canned feed.</summary>
class FakeUpdateSource(VelopackAssetFeed feed, Exception? exception = null) : IUpdateSource
{
    public Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel, Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
    {
        if (exception is not null)
        {
            throw exception;
        }

        return Task.FromResult(feed);
    }

    public Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile, Action<int> progress, CancellationToken cancelToken = default)
    {
        File.WriteAllBytes(localFile, []);
        return Task.CompletedTask;
    }
}
