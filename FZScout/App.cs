using FZScout.Logs;
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
    readonly LogWatcher _logWatcher;
    readonly Scout _scout;

    public App(AppDependencies dependencies)
    {
        Dependencies = dependencies;
        var log = dependencies.Log;
        var serverListCache = new ServerListCache(dependencies.FangbreakerZoneClient, log);
        this._bonusReporter = new BonusReporter(dependencies.FangbreakerZoneClient, serverListCache, log);
        this._apiKeyPrompt = new ApiKeyPrompt(this._bonusReporter, dependencies.ApiKeyStore, log, dependencies.UserInput.ReadApiKeyAsync);
        this._logWatcher = new LogWatcher(log);
        this._scout = new Scout(this._logWatcher, serverListCache, this._bonusReporter, dependencies.FindEqProcesses);
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
    }

    public async Task StartAsync()
    {
        Dependencies.Log.Info($"FZScout v{Dependencies.GetVersion()}");
        await Dependencies.GitHubUpdateChecker.CheckAsync(Dependencies.GetVersion(), Dependencies.Log, Dependencies.UserInput.ExitRequested);
        await this._bonusReporter.ChangeApiKeyAsync(Dependencies.ApiKeyStore.Load(), Dependencies.UserInput.ExitRequested);
        await TickScoutAsync();
        PrintWatchedLogs();
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

    void PrintWatchedLogs()
    {
        if (this._logWatcher.WatchedLogs.Count == 0)
        {
            Dependencies.Log.Info("No EverQuest logs found yet. Looking every 30 seconds while EverQuest runs.");
            return;
        }

        foreach (var folder in this._logWatcher.WatchedLogs.Select(Path.GetDirectoryName).Distinct())
        {
            Dependencies.Log.Info($"EverQuest logs found in {folder}");
        }
    }
}
