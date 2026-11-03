using FZScout.Scouting;
using FZScout.Site;

namespace FZScout.Reporting;

/// <summary>Carries out what the scouts decide, following the Reporting section of InternalDocumentation/SPEC.md.</summary>
class BonusReporter(FangbreakerZoneClient fangbreakerZoneClient, ApiKey apiKey, ConsoleReporter log)
{
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
        if (action is ReportNoBonusAction && !await SiteHasNoBonusAsync(report, cancellationToken))
        {
            return;
        }

        var result = await fangbreakerZoneClient.ReportBonusAsync(report, apiKey, cancellationToken);
        if (result.IsSuccess)
        {
            var reason = action is ReportNoBonusAction ? " because no bonus message arrived in time" : "";
            log.Success($"Reported {report}{reason}.");
            return;
        }

        log.Error($"Couldn't report {report}. {result.Error}");
        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            throw new ApiKeyRejectedException();
        }
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
}
