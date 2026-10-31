using FZScout.Scouting;
using FZScout.Site;

namespace FZScout.Reporting;

/// <summary>Carries out what the scouts decide, following the Reporting section of InternalDocumentation/SPEC.md.</summary>
class BonusReporter(FangbreakerZoneClient fangbreakerZoneClient, ServerListCache serverListCache, ConsoleLog log)
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    /// <summary>Checks a key by posting 'none' for a zone that never has a bonus.</summary>
    public static readonly BonusReport KeyCheck = new(Server.Fangbreaker, "The Nexus", BonusKey.None);

    readonly HashSet<Server> _banned = [];
    readonly Dictionary<(Character Character, string Zone), PendingRetry> _retries = [];
    ApiKey? _apiKey;
    bool _keyRejected;

    /// <summary>Raised when there's no key or the site rejected it, so the user should be asked for one.</summary>
    public event Action ApiKeyNeeded = delegate { };

    /// <summary>Changing the key resumes reporting after the old one was rejected.</summary>
    public void SetApiKey(ApiKey? apiKey)
    {
        this._apiKey = apiKey;
        this._keyRejected = false;
    }

    /// <summary>Sets the key and checks it with the site, logging whether it's working, rejected, or missing.</summary>
    public async Task ChangeApiKeyAsync(ApiKey? apiKey, CancellationToken cancellationToken = default)
    {
        SetApiKey(apiKey);
        if (apiKey is null)
        {
            log.Info("API key missing. Nothing will be reported.");
            ApiKeyNeeded();
            return;
        }

        var result = await fangbreakerZoneClient.ReportBonusAsync(KeyCheck, apiKey.Value, cancellationToken);
        if (result.IsSuccess)
        {
            log.Info("API key verified.");
            return;
        }

        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            await StopUntilKeyChanges();
            return;
        }

        log.Warning($"API key: couldn't check it, so it will be used as is. {result.Error}");
    }

    public Task ExecuteAsync(ScoutAction action, DateTimeOffset now, CancellationToken cancellationToken = default) => action switch
    {
        LogErrorAction a => LogErrorAsync(a.Message),
        LogInfoAction a => LogInfoAsync(a.Message),
        DropPendingReportsAction a => DropRetriesAsync(a.Character),
        ReportBonusAction or ReportNoBonusAction => AttemptAsync(action, now, isRetry: false, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    /// <summary>Call regularly. Sends retries that are due.</summary>
    public async Task TickAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var due = this._retries.Where(r => r.Value.At <= now).ToList();
        foreach (var (key, retry) in due)
        {
            this._retries.Remove(key);
            await AttemptAsync(retry.Action, now, isRetry: true, cancellationToken);
        }
    }

    static (Character Character, BonusReport Report) Describe(ScoutAction action) => action switch
    {
        ReportBonusAction a => (a.Character, a.Report),
        ReportNoBonusAction a => (a.Character, new BonusReport(a.Character.Server, a.Zone, BonusKey.None)),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    Task DropRetriesAsync(Character character)
    {
        foreach (var key in this._retries.Keys.Where(k => k.Character == character).ToList())
        {
            this._retries.Remove(key);
        }

        return Task.CompletedTask;
    }

    Task LogInfoAsync(string message)
    {
        log.Info(message);
        return Task.CompletedTask;
    }

    Task LogErrorAsync(string message)
    {
        log.Error(message);
        return Task.CompletedTask;
    }

    async Task AttemptAsync(ScoutAction action, DateTimeOffset now, bool isRetry, CancellationToken cancellationToken)
    {
        var (character, report) = Describe(action);

        // A newer report for the same character and zone replaces one waiting to retry.
        this._retries.Remove((character, report.Zone));

        if (!CanReport(report))
        {
            return;
        }

        if (action is ReportNoBonusAction && !await SiteHasNoBonusAsync(report, cancellationToken))
        {
            return;
        }

        var result = await fangbreakerZoneClient.ReportBonusAsync(report, this._apiKey!.Value, cancellationToken);
        if (result.IsSuccess)
        {
            var reason = action is ReportNoBonusAction ? " because no bonus message arrived in time" : "";
            log.Success($"Reported {report}{reason}.");
            return;
        }

        log.Error($"Couldn't report {report}. {result.Error}");
        await HandleFailureAsync(result.Outcome, action, character, report, now, isRetry, cancellationToken);
    }

    bool CanReport(BonusReport report)
    {
        if (this._apiKey is null)
        {
            log.Warning($"No API key, so {report} wasn't reported.");
            return false;
        }

        if (this._keyRejected)
        {
            log.Warning($"The API key was rejected, so {report} wasn't reported.");
            return false;
        }

        if (this._banned.Contains(report.Server))
        {
            log.Warning($"{report.Server.Site} has banned this account, so {report} wasn't reported.");
            return false;
        }

        return true;
    }

    /// <summary>True unless the site already shows a bonus for the zone, which we assume is right.</summary>
    async Task<bool> SiteHasNoBonusAsync(BonusReport report, CancellationToken cancellationToken)
    {
        var today = await fangbreakerZoneClient.GetTodayAsync(report.Server, cancellationToken);
        if (!today.IsSuccess)
        {
            log.Error($"Couldn't check today's bonuses, so {report} wasn't reported. {today.Error}");
            return false;
        }

        var shown = today.Value!.GetValueOrDefault(report.Zone, BonusKey.Unconfirmed);
        if (shown != BonusKey.None && shown != BonusKey.Unconfirmed)
        {
            log.Info($"{report.Server.Site} already shows '{shown}' for '{report.Zone}', so 'none' wasn't reported.");
            return false;
        }

        return true;
    }

    Task HandleFailureAsync(ApiOutcome outcome, ScoutAction action, Character character, BonusReport report, DateTimeOffset now, bool isRetry, CancellationToken cancellationToken) => outcome switch
    {
        ApiOutcome.KeyRejected => StopUntilKeyChanges(),
        ApiOutcome.Banned => StopReportingTo(report.Server),
        ApiOutcome.NotFound => serverListCache.ReloadAsync(report.Server, now, cancellationToken),
        ApiOutcome.Unavailable => RetryOnce(action, character, report, now, isRetry),
        _ => Task.CompletedTask,
    };

    Task StopUntilKeyChanges()
    {
        this._keyRejected = true;
        log.Error("API key: rejected. Reporting is stopped until the key changes.");
        ApiKeyNeeded();
        return Task.CompletedTask;
    }

    Task StopReportingTo(Server server)
    {
        this._banned.Add(server);
        log.Error($"{server.Site} has banned or timed out this account. Reporting to it is stopped until restart.");
        return Task.CompletedTask;
    }

    Task RetryOnce(ScoutAction action, Character character, BonusReport report, DateTimeOffset now, bool isRetry)
    {
        if (isRetry)
        {
            log.Error($"Gave up reporting {report}.");
        }
        else
        {
            this._retries[(character, report.Zone)] = new PendingRetry(action, now + RetryDelay);
        }

        return Task.CompletedTask;
    }

    record PendingRetry(ScoutAction Action, DateTimeOffset At);
}
