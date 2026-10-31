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
    readonly UpdateChecker _updateChecker;

    public App(AppDependencies dependencies)
    {
        this.Dependencies = dependencies;
        var log = dependencies.Log;
        var serverListCache = new ServerListCache(dependencies.FangbreakerZoneClient, log);
        this._bonusReporter = new BonusReporter(dependencies.FangbreakerZoneClient, serverListCache, log);
        this._apiKeyPrompt = new ApiKeyPrompt(this._bonusReporter, dependencies.ApiKeyStore, log, dependencies.ReadApiKeyAsync);
        this._logWatcher = new LogWatcher(log);
        this._scout = new Scout(this._logWatcher, serverListCache, this._bonusReporter, dependencies.FindEqProcesses);
        this._updateChecker = new UpdateChecker(log, dependencies.GitHubClient);
    }

    public AppDependencies Dependencies { get; }

    /// <summary>Unless FZScout is already running, starts, then ticks every <see cref="TickInterval"/> until the user asks to exit.</summary>
    public async Task RunAsync()
    {
        using var singleInstanceClaim = this.Dependencies.TryClaimSingleInstance();
        if (singleInstanceClaim is null)
        {
            this.Dependencies.Log.Error("FZScout is already running.");
            this.Dependencies.Log.Info("Press any key to exit.");
            this.Dependencies.WaitForAnyKey();
            return;
        }

        await StartAsync();

        using var timer = new PeriodicTimer(TickInterval, this.Dependencies.TimeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(this.Dependencies.ExitRequested))
            {
                await TickAsync();
            }
        }
        catch (OperationCanceledException) when (this.Dependencies.ExitRequested.IsCancellationRequested)
        {
        }
    }

    public async Task StartAsync()
    {
        this.Dependencies.Log.Info($"FZScout v{this.Dependencies.GetVersion()}");
        await this._updateChecker.CheckAsync(this.Dependencies.GetVersion(), this.Dependencies.ExitRequested);
        await this._bonusReporter.ChangeApiKeyAsync(this.Dependencies.ApiKeyStore.Load(), this.Dependencies.ExitRequested);
        await TickScoutAsync();
        PrintWatchedLogs();
        this.Dependencies.Log.Info("Press Ctrl+C to exit.");
    }

    public async Task TickAsync()
    {
        await this._apiKeyPrompt.TickAsync(this.Dependencies.ExitRequested);
        await TickScoutAsync();
    }

    /// <summary>One bad tick is logged, not fatal: the next one gets a fresh start.</summary>
    async Task TickScoutAsync()
    {
        try
        {
            await this._scout.TickAsync(this.Dependencies.TimeProvider.GetUtcNow(), this.Dependencies.ExitRequested);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            this.Dependencies.Log.Error($"Unexpected problem: {exception}");
        }
    }

    void PrintWatchedLogs()
    {
        if (this._logWatcher.WatchedLogs.Count == 0)
        {
            this.Dependencies.Log.Info("No EverQuest logs found yet. Looking every 30 seconds while EverQuest runs.");
            return;
        }

        foreach (var folder in this._logWatcher.WatchedLogs.Select(Path.GetDirectoryName).Distinct())
        {
            this.Dependencies.Log.Info($"EverQuest logs found in {folder}");
        }
    }
}
