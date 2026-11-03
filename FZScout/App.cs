using FZScout.Reporting;

namespace FZScout;

/// <summary>
/// The whole app, wired up from everything it needs from outside: the clock, the console, the sites, and so on.
/// Tests pass in-memory versions of those.
/// </summary>
class App
{
    public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

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

    /// <summary>Unless FZScout is already running, starts, then ticks every <see cref="TickInterval"/> until the user asks to exit.</summary>
    public async Task RunAsync()
    {
        using var singleInstanceClaim = Dependencies.TryClaimSingleInstance();
        if (singleInstanceClaim is null)
        {
            Dependencies.UserInput.InformUserAppIsAlreadyRunning();
            return;
        }

        await StartAsync();

        var watchingLogs = WatchLogsAsync(Dependencies.UserInput.ExitRequested);
        using var timer = new PeriodicTimer(TickInterval, Dependencies.TimeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(Dependencies.UserInput.ExitRequested))
            {
                await TickAsync();
            }
        }
        catch (OperationCanceledException) when (Dependencies.UserInput.ExitRequested.IsCancellationRequested)
        {
        }

        await watchingLogs;
    }

    public async Task StartAsync()
    {
        var downloadedUpdateVersion = await Dependencies.GitHubUpdateChecker.CheckAsync(Dependencies.UserInput.ExitRequested);
        var versionLine = $"FZScout v{Dependencies.GetVersion()}";
        if (downloadedUpdateVersion is not null)
        {
            Dependencies.Log.Success($"{versionLine} (v{downloadedUpdateVersion} downloaded, applies on restart).");
        }
        else
        {
            Dependencies.Log.Info(versionLine);
        }

        await this._bonusReporter.ChangeApiKeyAsync(Dependencies.ApiKeyStore.Load(), Dependencies.UserInput.ExitRequested);
        await TickScoutAsync();
        Dependencies.Log.Info("Press Ctrl+C to exit.");
    }

    public async Task TickAsync()
    {
        await this._apiKeyPrompt.TickAsync(Dependencies.UserInput.ExitRequested);
        await TickScoutAsync();
    }

    /// <summary>One bad tick is logged, not fatal: the next one gets a fresh start.</summary>
    async Task TickScoutAsync()
    {
        try
        {
            await this._scout.TickAsync(Dependencies.TimeProvider.GetUtcNow(), Dependencies.UserInput.ExitRequested);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Dependencies.Log.Error($"Unexpected problem: {exception}");
        }
    }

    /// <summary>Runs for the life of the app: feeds every event EverQuestLogWatcher finds to the scout. One bad event is logged, not fatal.</summary>
    async Task WatchLogsAsync(CancellationToken cancellationToken)
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
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
