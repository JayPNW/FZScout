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
        if (await GetAcceptedApiKeyAsync() is not { } apiKey)
        {
            return;
        }
        Dependencies.Log.Info("To exit, right-click the FZScout icon in the system tray and choose Exit.");

        var scout = CreateScoutReportingWith(apiKey);
        ScoutingStarted?.Invoke();
        await WatchLogsAndReportBonusesAsync(scout, Dependencies.UserInput.ExitRequested);
    }

    Scout CreateScoutReportingWith(ApiKey apiKey)
    {
        var client = Dependencies.FangbreakerZoneClient;
        var scout = new Scout(new ServerListCache(client, Dependencies.Log), Dependencies.Log, new BonusReporter(client, apiKey, Dependencies.Log));
        scout.WaitingForBonusMessageUntilChanged += waitingUntil => WaitingForBonusMessageUntilChanged?.Invoke(waitingUntil);
        return scout;
    }

    /// <returns>Null if the user entered no key or asked to exit.</returns>
    public async Task<ApiKey?> GetAcceptedApiKeyAsync()
    {
        try
        {
            var apiKeyPrompt = new ApiKeyPrompt(Dependencies.FangbreakerZoneClient, Dependencies.ApiKeyStore, Dependencies.Log, Dependencies.UserInput.ReadApiKeyAsync);
            return await apiKeyPrompt.GetAcceptedApiKeyAsync(Dependencies.UserInput.ExitRequested);
        }
        catch (OperationCanceledException) when (Dependencies.UserInput.ExitRequested.IsCancellationRequested)
        {
            return null;
        }
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
        catch (ApiKeyRejectedException)
        {
            Dependencies.Log.Error("API key rejected. Exiting.");
        }
    }

    async Task HandleLogEventLoggingUnexpectedProblemsAsync(Scout scout, EverQuestLogWatcher.EverQuestLogEvent logEvent, CancellationToken cancellationToken)
    {
        try
        {
            await scout.HandleAsync(logEvent, Dependencies.TimeProvider.GetUtcNow(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not ApiKeyRejectedException)
        {
            Dependencies.Log.Error($"Unexpected problem: {exception}");
        }
    }
}
