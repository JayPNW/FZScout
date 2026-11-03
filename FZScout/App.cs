using FZScout.Reporting;

namespace FZScout;

/// <summary>
/// The whole app, wired up from everything it needs from outside: the clock, the console, the sites, and so on.
/// Tests pass in-memory versions of those.
/// </summary>
class App
{
    readonly BonusReporter _bonusReporter;
    readonly ApiKeyPrompt _apiKeyPrompt;
    readonly Scout _scout;

    public App(AppDependencies dependencies)
    {
        Dependencies = dependencies;
        var log = dependencies.Log;
        var serverListCache = new ServerListCache(dependencies.FangbreakerZoneClient, log);
        this._bonusReporter = new BonusReporter(dependencies.FangbreakerZoneClient, serverListCache, log);
        this._apiKeyPrompt = new ApiKeyPrompt(this._bonusReporter, dependencies.ApiKeyStore, log, dependencies.UserInput.ReadApiKeyAsync);
        this._scout = new Scout(serverListCache, this._bonusReporter);
    }

    public AppDependencies Dependencies { get; }

    public async Task RunAsync()
    {
        using var singleInstanceClaim = Dependencies.TryClaimSingleInstance();
        if (singleInstanceClaim is null)
        {
            Dependencies.UserInput.InformUserAppIsAlreadyRunning();
            return;
        }

        Dependencies.Log.Info($"FZScout v{Dependencies.GetVersion()}");
        if (!await EnsureHaveApiKey())
        {
            return;
        }
        Dependencies.Log.Info("Press Ctrl+C to exit.");

        await WatchLogsAndReportBonusesAsync(Dependencies.UserInput.ExitRequested);
    }

    public async Task<bool> EnsureHaveApiKey()
    {
        try
        {
            await this._bonusReporter.ChangeApiKeyAsync(Dependencies.ApiKeyStore.Load(), Dependencies.UserInput.ExitRequested);
            await this._apiKeyPrompt.AskUntilAcceptedAsync(Dependencies.UserInput.ExitRequested);
        }
        catch (OperationCanceledException) when (Dependencies.UserInput.ExitRequested.IsCancellationRequested)
        {
            return false;
        }

        return this._bonusReporter.HasApiKey;
    }

    /// <summary>
    /// Runs for the life of the app: feeds every event EverQuestLogWatcher finds to the scout. One bad event is logged, not fatal.
    /// Stops if the site rejects the API key.
    /// </summary>
    async Task WatchLogsAndReportBonusesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var logEvent in new EverQuestLogWatcher.EverQuestLogWatcher().WatchAsync(cancellationToken: cancellationToken))
            {
                try
                {
                    await this._scout.HandleAsync(logEvent, Dependencies.TimeProvider.GetUtcNow(), cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Dependencies.Log.Error($"Unexpected problem: {exception}");
                }

                if (!this._bonusReporter.HasApiKey)
                {
                    Dependencies.Log.Info("Exiting.");
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
