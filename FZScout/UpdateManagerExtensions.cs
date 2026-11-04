using Velopack;

namespace FZScout;

/// <summary>
/// Stands in for an update-and-apply-on-dispose method we expect Velopack to add; switch callers to that once it exists.
/// </summary>
static class UpdateManagerExtensions
{
    public static async Task<IDisposable?> DownloadLatestAndApplyOnDispose(this UpdateManager manager, CancellationToken cancellationToken = default)
    {
        var update = await manager.CheckForUpdatesAsync();
        if (update is not null)
        {
            await manager.DownloadUpdatesAsync(update, cancelToken: cancellationToken);
            return new PendingUpdate(manager, update);
        }

        return null;
    }

    class PendingUpdate(UpdateManager manager, UpdateInfo update) : IDisposable
    {
        public void Dispose() => manager.WaitExitThenApplyUpdates(update, restart: false);
    }
}
