using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace FZScout.Tests;

/// <summary>Stands in for GitHub: answers a release check with a canned feed, which a test can change to publish a new release.</summary>
class FakeUpdateSource(VelopackAssetFeed feed, Exception? exception = null) : IUpdateSource
{
    public VelopackAssetFeed Feed { get; set; } = feed;

    public Exception? ExceptionToThrowWhenChecked { get; set; } = exception;

    public Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel, Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
    {
        if (this.ExceptionToThrowWhenChecked is not null)
        {
            throw this.ExceptionToThrowWhenChecked;
        }

        return Task.FromResult(this.Feed);
    }

    public Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile, Action<int> progress, CancellationToken cancelToken = default)
    {
        File.WriteAllBytes(localFile, []);
        return Task.CompletedTask;
    }
}
