using FZScout.Site;

namespace FZScout.Reporting;

/// <summary>Sends the scouts' reports to the site, following the Reporting section of InternalDocumentation/SPEC.md.</summary>
class BonusReporter(FangbreakerZoneClient fangbreakerZoneClient, ApiKey apiKey, Log log)
{
    public Task ReportBonusAsync(BonusReport report, CancellationToken cancellationToken = default)
    {
        return PostAsync(report, $"Reported {report}.", cancellationToken);
    }

    public async Task ReportNoBonusAsync(Server server, string zone, CancellationToken cancellationToken = default)
    {
        var report = new BonusReport(server, zone, BonusKey.None);
        if (!await SiteHasNoBonusAsync(report, cancellationToken))
        {
            return;
        }

        await PostAsync(report, $"Reported {report} because no bonus message arrived in time.", cancellationToken);
    }

    async Task PostAsync(BonusReport report, string successMessage, CancellationToken cancellationToken)
    {
        var result = await fangbreakerZoneClient.ReportBonusAsync(report, apiKey, cancellationToken);
        if (result.IsSuccess)
        {
            log.Success(successMessage);
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
