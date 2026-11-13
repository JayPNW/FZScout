using System.Net;
using System.Text.Json;
using FZApi;

namespace FZScout.Site;

/// <summary>Talks to fangbreaker.zone and frostreaver.zone through <see cref="FZApiClient"/>, turning its exceptions into <see cref="ApiResult{T}"/>.</summary>
class FangbreakerZoneClient(HttpMessageHandler httpMessageHandler)
{
    const string UserAgent = "FZScout";

    /// <summary>Checks a key by posting 'none' for a zone that never has a bonus.</summary>
    static readonly BonusReport ApiKeyCheck = new(Server.Fangbreaker, "The Nexus", BonusKey.None);

    readonly HttpClient _httpClient = new(httpMessageHandler) { Timeout = TimeSpan.FromSeconds(30) };

    public FangbreakerZoneClient()
        : this(new HttpClientHandler())
    {
    }

    public async Task<ApiResult<IReadOnlySet<BonusKey>>> GetBonusTypesAsync(Server server, CancellationToken cancellationToken = default)
    {
        return await CallAsync(
            $"GET {BonusListUri(server, "types")}",
            async () => (IReadOnlySet<BonusKey>)(await ClientFor(server).GetBonusTypesAsync(Slug(server), cancellationToken)).Select(t => new BonusKey(t.Key)).ToHashSet(),
            cancellationToken);
    }

    public async Task<ApiResult<IReadOnlySet<string>>> GetZonesAsync(Server server, CancellationToken cancellationToken = default)
    {
        return await CallAsync(
            $"GET {BonusListUri(server, "zones")}",
            async () => (IReadOnlySet<string>)(await ClientFor(server).GetZonesAsync(Slug(server), cancellationToken)).Select(z => z.Name).ToHashSet(),
            cancellationToken);
    }

    /// <summary>Today's bonus for each zone, keyed by the site's zone name. Zones nobody has reported are <see cref="BonusKey.Unconfirmed"/>.</summary>
    public async Task<ApiResult<IReadOnlyDictionary<string, BonusKey>>> GetTodayAsync(Server server, CancellationToken cancellationToken = default)
    {
        return await CallAsync(
            $"GET {BonusListUri(server, "today")}",
            async () => (IReadOnlyDictionary<string, BonusKey>)(await ClientFor(server).GetTodaysBonusesAsync(Slug(server), cancellationToken)).DistinctBy(z => z.Name).ToDictionary(z => z.Name, z => new BonusKey(z.Bonus)),
            cancellationToken);
    }

    public async Task<ApiResult<bool>> ReportBonusAsync(BonusReport report, ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var siteReport = new FZApi.Models.BonusReport { ServerName = report.Server.Name, ZoneName = report.Zone, BonusKey = report.Bonus.Value };
        return await CallAsync(
            $"POST {new Uri(report.Server.Site, "/api/client/bonuses")}",
            async () => (await ClientFor(report.Server, apiKey).ReportBonusAsync(siteReport, cancellationToken)).Updated,
            cancellationToken);
    }

    public Task<ApiResult<bool>> CheckApiKeyAsync(ApiKey apiKey, CancellationToken cancellationToken = default) => ReportBonusAsync(ApiKeyCheck, apiKey, cancellationToken);

    static string Slug(Server server) => server.Name.ToLowerInvariant();

    static Uri BonusListUri(Server server, string list) => new(server.Site, $"/api/servers/{Slug(server)}/bonuses/{list}");

    static async Task<ApiResult<T>> CallAsync<T>(string requestDescription, Func<Task<T>> call, CancellationToken cancellationToken)
    {
        try
        {
            var value = await call();
            if (value is null)
            {
                return ApiResult<T>.Failure(ApiOutcome.Failed, $"{requestDescription} returned no data.");
            }

            return ApiResult<T>.Success(value);
        }
        catch (FZApiException exception) when (exception.StatusCode != HttpStatusCode.OK)
        {
            return ApiResult<T>.Failure(OutcomeFor(exception.StatusCode), $"{requestDescription} returned {(int)exception.StatusCode}: {exception.ResponseBody}");
        }
        catch (FZApiException exception)
        {
            return ApiResult<T>.Failure(ApiOutcome.Failed, $"{requestDescription} returned an unexpected body: {exception.ResponseBody}");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Failure(ApiOutcome.Failed, $"{requestDescription} failed: {exception.Message}");
        }
    }

    static ApiOutcome OutcomeFor(HttpStatusCode httpStatusCode) => httpStatusCode switch
    {
        HttpStatusCode.Unauthorized => ApiOutcome.KeyRejected,
        _ => ApiOutcome.Failed,
    };

    FZApiClient ClientFor(Server server, ApiKey? apiKey = null) => new(this._httpClient, new FZApiClientOptions { BaseAddress = server.Site, ApiKey = apiKey?.Value, UserAgent = UserAgent });
}
