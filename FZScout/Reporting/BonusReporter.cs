using FZScout.Scouting;
using FZScout.Site;

namespace FZScout.Reporting;

/// <summary>Carries out what the scouts decide, following the Reporting section of InternalDocumentation/SPEC.md.</summary>
class BonusReporter(FangbreakerZoneClient fangbreakerZoneClient, ConsoleReporter log)
{
    /// <summary>Checks a key by posting 'none' for a zone that never has a bonus.</summary>
    public static readonly BonusReport KeyCheck = new(Server.Fangbreaker, "The Nexus", BonusKey.None);

    ApiKey? _apiKey;
    bool _keyRejected;

    /// <summary>True once a key has been set and hasn't been rejected by the site.</summary>
    public bool HasApiKey => this._apiKey is not null && !this._keyRejected;

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
            return;
        }

        log.Info($"Checking the API key with {KeyCheck.Server.Site.Host}.");
        var result = await fangbreakerZoneClient.ReportBonusAsync(KeyCheck, apiKey.Value, cancellationToken);
        if (result.IsSuccess)
        {
            log.Info("API key verified.");
            return;
        }

        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            log.Error(result.Error!);
            StopUntilKeyChanges();
            return;
        }

        log.Warning($"API key: couldn't check it, so it will be used as is. {result.Error}");
    }

    public Task ExecuteAsync(ScoutAction action, CancellationToken cancellationToken = default) => action switch
    {
        LogErrorAction a => LogErrorAsync(a.Message),
        LogInfoAction a => LogInfoAsync(a.Message),
        ReportAction a => AttemptAsync(a, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

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

    async Task AttemptAsync(ReportAction action, CancellationToken cancellationToken)
    {
        var report = action.Report;
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
        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            StopUntilKeyChanges();
        }
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

    void StopUntilKeyChanges()
    {
        this._keyRejected = true;
        log.Error("API key rejected.");
    }
}
