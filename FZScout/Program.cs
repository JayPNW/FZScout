using System.Reflection;
using FZScout.Logs;
using FZScout.Reporting;
using FZScout.Site;

namespace FZScout;

static class Program
{
    static async Task Main()
    {
        var timeProvider = TimeProvider.System;
        var log = ConsoleLog.ForConsole(timeProvider);

        using var mutex = new Mutex(true, "FZScout", out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            log.Error("FZScout is already running.");
            log.Info("Press any key to exit.");
            Console.ReadKey(intercept: true);
            return;
        }

        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        log.Info($"FZScout v{version}");

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var fangbreakerZoneClient = new FangbreakerZoneClient(httpClient);
        var serverListCache = new ServerListCache(fangbreakerZoneClient, log);
        var bonusReporter = new BonusReporter(fangbreakerZoneClient, serverListCache, log);
        var prompt = new ApiKeyPrompt(bonusReporter, log);
        var logWatcher = new LogWatcher(log);
        var scout = new Scout(logWatcher, serverListCache, bonusReporter, EqProcesses.Find);

        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancel.Cancel();
        };

        await new UpdateChecker(log, httpClient).CheckAsync(version, cancel.Token);
        await bonusReporter.ChangeApiKeyAsync(ApiKeyStore.Load(), cancel.Token);
        await TickAsync(scout, timeProvider, log, cancel.Token);
        PrintWatchedLogs(logWatcher, log);
        log.Info("Press Ctrl+C to exit.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancel.Token))
            {
                await prompt.TickAsync(cancel.Token);
                await TickAsync(scout, timeProvider, log, cancel.Token);
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
    }

    /// <summary>One bad tick is logged, not fatal: the next one gets a fresh start.</summary>
    static async Task TickAsync(Scout scout, TimeProvider timeProvider, ConsoleLog log, CancellationToken cancellationToken)
    {
        try
        {
            await scout.TickAsync(timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.Error($"Unexpected problem: {exception}");
        }
    }

    static void PrintWatchedLogs(LogWatcher logWatcher, ConsoleLog log)
    {
        if (logWatcher.WatchedLogs.Count == 0)
        {
            log.Info("No EverQuest logs found yet. Looking every 30 seconds while EverQuest runs.");
            return;
        }

        foreach (var folder in logWatcher.WatchedLogs.Select(Path.GetDirectoryName).Distinct())
        {
            log.Info($"EverQuest logs found in {folder}");
        }
    }
}
