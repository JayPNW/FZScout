using EverQuestLogWatcher;

namespace FZScout;

/// <summary>Notices when a running EverQuest has logging turned off, since FZScout can't see its messages then.</summary>
class EverQuestLoggingMonitor(Func<IReadOnlyList<RunningEverQuestInstall>> findRunningEverQuestInstalls, Log log, TimeProvider timeProvider)
{
    static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    bool _isEverQuestLoggingEnabled = true;

    public event Action<bool>? IsEverQuestLoggingEnabledChanged;

    /// <summary>Logs only when it changes, so a player who leaves logging off isn't told over and over.</summary>
    public void CheckWhetherEverQuestLoggingIsEnabled()
    {
        var installsWithLoggingOff = findRunningEverQuestInstalls().Where(install => !install.IsLoggingEnabled).ToList();
        var isEverQuestLoggingEnabled = installsWithLoggingOff.Count == 0;
        if (isEverQuestLoggingEnabled == this._isEverQuestLoggingEnabled)
        {
            return;
        }

        this._isEverQuestLoggingEnabled = isEverQuestLoggingEnabled;
        LogWhetherEverQuestLoggingIsEnabled(installsWithLoggingOff);
        IsEverQuestLoggingEnabledChanged?.Invoke(isEverQuestLoggingEnabled);
    }

    public async Task CheckRepeatedlyAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                CheckWhetherEverQuestLoggingIsEnabledLoggingUnexpectedProblems();
                await Task.Delay(CheckInterval, timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
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

    void CheckWhetherEverQuestLoggingIsEnabledLoggingUnexpectedProblems()
    {
        try
        {
            CheckWhetherEverQuestLoggingIsEnabled();
        }
        catch (Exception exception)
        {
            log.Error($"Unexpected problem checking whether EverQuest logging is on: {exception}");
        }
    }
}
