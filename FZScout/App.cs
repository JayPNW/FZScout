using FZScout.Reporting;
using FZScout.Scouting;
using FZScout.Site;

namespace FZScout;

/// <summary>
/// The whole app, wired up from everything it needs from outside: the clock, the window, the sites, and so on.
/// Tests pass in-memory versions of those.
/// </summary>
class App(AppDependencies dependencies)
{
    /// <summary>The time can already have passed, since a wait is only over once the character's next message arrives.</summary>
    public event Action<DateTimeOffset?>? WaitingForBonusMessageUntilChanged;

    public event Action? ScoutingStarted;

    public event Action<BonusShownOnSite?>? BonusShownOnSiteForCurrentZoneChanged;

    public event Action? ApiKeyRejected;

    public event Action<bool>? IsEverQuestLoggingEnabledChanged;

    public event Action<bool>? IsEverQuestRunningChanged;

    public AppDependencies Dependencies => dependencies;

    public async Task RunAsync()
    {
        using var singleInstanceClaim = Dependencies.TryClaimSingleInstance();
        if (singleInstanceClaim is null)
        {
            Dependencies.UserInput.InformUserAppIsAlreadyRunning();
            return;
        }

        Dependencies.Log.Info($"FZScout v{Dependencies.GetVersion()}");
        Dependencies.Log.Info("To exit, right-click the FZScout icon in the system tray and choose Exit.");
        await LogProblemWithStoredApiKeyAsync(Dependencies.UserInput.ExitRequested);

        var scout = CreateScout();
        ScoutingStarted?.Invoke();
        var watchingRunningEverQuest = CreateRunningEverQuestMonitor().CheckRepeatedlyAsync(Dependencies.UserInput.ExitRequested);
        await WatchLogsAndReportBonusesAsync(scout, Dependencies.UserInput.ExitRequested);
        await watchingRunningEverQuest;
    }

    /// <summary>The only time the key is checked without the user asking, so a bad one shows up before the first report.</summary>
    public async Task LogProblemWithStoredApiKeyAsync(CancellationToken cancellationToken)
    {
        if (Dependencies.ApiKeyStore.Load() is not { } apiKey)
        {
            Dependencies.Log.Error("No API key. Enter one in Settings.");
            return;
        }

        var result = await Dependencies.FangbreakerZoneClient.CheckApiKeyAsync(apiKey, cancellationToken);
        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            Dependencies.Log.Error($"API key rejected. Enter a new one in Settings. {result.Error}");
            ApiKeyRejected?.Invoke();
        }
    }

    RunningEverQuestMonitor CreateRunningEverQuestMonitor()
    {
        var monitor = new RunningEverQuestMonitor(Dependencies.FindRunningEverQuestInstalls, Dependencies.Log, Dependencies.TimeProvider);
        monitor.IsEverQuestLoggingEnabledChanged += isEnabled => IsEverQuestLoggingEnabledChanged?.Invoke(isEnabled);
        monitor.IsEverQuestRunningChanged += isRunning => IsEverQuestRunningChanged?.Invoke(isRunning);
        return monitor;
    }

    Scout CreateScout()
    {
        var client = Dependencies.FangbreakerZoneClient;
        var bonusReporter = new BonusReporter(client, Dependencies.ApiKeyStore.Load, Dependencies.Log);
        bonusReporter.ApiKeyRejected += () => ApiKeyRejected?.Invoke();
        var scout = new Scout(new ServerListCache(client, Dependencies.Log), Dependencies.Log, bonusReporter, client);
        scout.WaitingForBonusMessageUntilChanged += waitingUntil => WaitingForBonusMessageUntilChanged?.Invoke(waitingUntil);
        scout.BonusShownOnSiteForCurrentZoneChanged += shown => BonusShownOnSiteForCurrentZoneChanged?.Invoke(shown);
        return scout;
    }

    async Task WatchLogsAndReportBonusesAsync(Scout scout, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var logEvent in new EverQuestLogWatcher.EverQuestLogWatcher().WatchAsync(cancellationToken: cancellationToken))
            {
                await HandleLogEventLoggingUnexpectedProblemsAsync(scout, logEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task HandleLogEventLoggingUnexpectedProblemsAsync(Scout scout, EverQuestLogWatcher.EverQuestLogEvent logEvent, CancellationToken cancellationToken)
    {
        try
        {
            await scout.HandleAsync(logEvent, Dependencies.TimeProvider.GetUtcNow(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Dependencies.Log.Error($"Unexpected problem: {exception}");
        }
    }
}
