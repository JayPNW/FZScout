using EverQuestLogWatcher;

namespace FZScout;

/// <summary>Checks the running EverQuest clients: whether any is running, and whether any has logging turned off, since FZScout can't see its messages then.</summary>
class RunningEverQuestMonitor(Func<IReadOnlyList<RunningEverQuestInstall>> findRunningEverQuestInstalls, Log log, TimeProvider timeProvider)
{
    static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    bool _isEverQuestLoggingEnabled = true;

    bool _isEverQuestRunning;

    public event Action<bool>? IsEverQuestLoggingEnabledChanged;

    public event Action<bool>? IsEverQuestRunningChanged;

    public void CheckRunningEverQuests()
    {
        var runningEverQuestInstalls = findRunningEverQuestInstalls();
        CheckWhetherEverQuestIsRunning(runningEverQuestInstalls);
        CheckWhetherEverQuestLoggingIsEnabled(runningEverQuestInstalls);
    }

    public async Task CheckRepeatedlyAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                CheckRunningEverQuestsLoggingUnexpectedProblems();
                await Task.Delay(CheckInterval, timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    void CheckWhetherEverQuestIsRunning(IReadOnlyList<RunningEverQuestInstall> runningEverQuestInstalls)
    {
        var isEverQuestRunning = runningEverQuestInstalls.Count > 0;
        if (isEverQuestRunning == this._isEverQuestRunning)
        {
            return;
        }

        this._isEverQuestRunning = isEverQuestRunning;
        IsEverQuestRunningChanged?.Invoke(isEverQuestRunning);
    }

    /// <summary>Logs only when it changes, so a player who leaves logging off isn't told over and over.</summary>
    void CheckWhetherEverQuestLoggingIsEnabled(IReadOnlyList<RunningEverQuestInstall> runningEverQuestInstalls)
    {
        var installsWithLoggingOff = runningEverQuestInstalls.Where(install => !install.IsLoggingEnabled).ToList();
        var isEverQuestLoggingEnabled = installsWithLoggingOff.Count == 0;
        if (isEverQuestLoggingEnabled == this._isEverQuestLoggingEnabled)
        {
            return;
        }

        this._isEverQuestLoggingEnabled = isEverQuestLoggingEnabled;
        LogWhetherEverQuestLoggingIsEnabled(installsWithLoggingOff);
        IsEverQuestLoggingEnabledChanged?.Invoke(isEverQuestLoggingEnabled);
    }

    void LogWhetherEverQuestLoggingIsEnabled(List<RunningEverQuestInstall> installsWithLoggingOff)
    {
        if (installsWithLoggingOff.Count == 0)
        {
            log.Info("EverQuest logging is on.");
            return;
        }

        var directories = string.Join(", ", installsWithLoggingOff.Select(install => install.InstallDirectory));
        log.Warning($"EverQuest logging is off in {directories}, so FZScout can't see bonus messages. Type `/log` on in EverQuest.");
    }

    void CheckRunningEverQuestsLoggingUnexpectedProblems()
    {
        try
        {
            CheckRunningEverQuests();
        }
        catch (IOException)
        {
            // EverQuest briefly locks eqclient.ini while writing it. The next check, a few seconds from now, will see it.
        }
        catch (Exception exception)
        {
            log.Error($"Unexpected problem checking the running EverQuest clients: {exception}");
        }
    }
}
