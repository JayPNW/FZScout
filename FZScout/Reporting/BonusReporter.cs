using FZScout.Site;

namespace FZScout.Reporting;

/// <summary>Sends the scouts' reports to the site, following the Reporting section of InternalDocumentation/SPEC.md.</summary>
/// <param name="loadApiKey">Called for each report, so a key the user saves in Settings takes effect right away.</param>
class BonusReporter(FangbreakerZoneClient fangbreakerZoneClient, Func<ApiKey?> loadApiKey, Log log)
{
    public async Task ReportBonusAsync(BonusReport report, CancellationToken cancellationToken = default)
    {
        var shownOnSite = await GetBonusShownOnSiteAsync(report, cancellationToken);
        if (shownOnSite.IsSuccess && shownOnSite.Value == report.Bonus)
        {
            LogAlreadyShown(report);
            return;
        }

        await PostAsync(report, $"Reported {report}.", cancellationToken);
    }

    public async Task ReportNoBonusAsync(Server server, string zone, CancellationToken cancellationToken = default)
    {
        var report = new BonusReport(server, zone, BonusKey.None);
        var shownOnSite = await GetBonusShownOnSiteAsync(report, cancellationToken);
        if (!shownOnSite.IsSuccess)
        {
            log.Error($"Couldn't check today's bonuses, so {report} wasn't reported. {shownOnSite.Error}");
            return;
        }

        var shown = shownOnSite.Value;
        if (shown == BonusKey.None)
        {
            LogAlreadyShown(report);
            return;
        }

        if (shown != BonusKey.Unconfirmed)
        {
            log.Info($"{report.Server.Site} already shows '{shown}' for '{report.Zone}', so 'none' wasn't reported.");
            return;
        }

        await PostAsync(report, $"Reported {report} because no bonus message arrived in time.", cancellationToken);
    }

    async Task PostAsync(BonusReport report, string successMessage, CancellationToken cancellationToken)
    {
        if (loadApiKey() is not { } apiKey)
        {
            log.Error($"Couldn't report {report}, because there's no API key. Enter one in Settings.");
            return;
        }

        var result = await fangbreakerZoneClient.ReportBonusAsync(report, apiKey, cancellationToken);
        if (result.IsSuccess)
        {
            log.Success(successMessage);
            return;
        }

        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            log.Error($"Couldn't report {report}, because the API key was rejected. Enter a new one in Settings. {result.Error}");
            return;
        }

        log.Error($"Couldn't report {report}. {result.Error}");
    }

    /// <summary>What the site shows today for the report's zone; <see cref="BonusKey.Unconfirmed"/> if the zone isn't listed.</summary>
    async Task<ApiResult<BonusKey>> GetBonusShownOnSiteAsync(BonusReport report, CancellationToken cancellationToken)
    {
        var today = await fangbreakerZoneClient.GetTodayAsync(report.Server, cancellationToken);
        if (!today.IsSuccess)
        {
            return ApiResult<BonusKey>.Failure(today.Outcome, today.Error!);
        }

        return ApiResult<BonusKey>.Success(today.Value!.GetValueOrDefault(report.Zone, BonusKey.Unconfirmed));
    }

    void LogAlreadyShown(BonusReport report)
    {
        log.Info($"{report.Server.Site} already shows '{report.Bonus}' for '{report.Zone}', so it wasn't reported again.");
    }
}
