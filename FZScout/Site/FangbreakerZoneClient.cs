using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FZScout.Site;

/// <summary>Talks to fangbreaker.zone and frostreaver.zone. See InternalDocumentation/API.md.</summary>
class FangbreakerZoneClient(HttpMessageHandler httpMessageHandler)
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    readonly HttpClient _httpClient = new(httpMessageHandler) { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>Talks to the real sites.</summary>
    public FangbreakerZoneClient()
        : this(new HttpClientHandler())
    {
    }

    public async Task<ApiResult<IReadOnlySet<BonusKey>>> GetBonusTypesAsync(Server server, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<TypeDto[]>(server, "types", cancellationToken);
        return Map(result, types => (IReadOnlySet<BonusKey>)types.Select(t => new BonusKey(t.Key)).ToHashSet());
    }

    public async Task<ApiResult<IReadOnlySet<string>>> GetZonesAsync(Server server, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<ZoneDto[]>(server, "zones", cancellationToken);
        return Map(result, zones => (IReadOnlySet<string>)zones.Select(z => z.Name).ToHashSet());
    }

    /// <summary>Today's bonus for each zone, keyed by the site's zone name. Zones nobody has reported are <see cref="BonusKey.Unconfirmed"/>.</summary>
    public async Task<ApiResult<IReadOnlyDictionary<string, BonusKey>>> GetTodayAsync(Server server, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<TodayDto[]>(server, "today", cancellationToken);
        return Map(result, zones => (IReadOnlyDictionary<string, BonusKey>)zones.DistinctBy(z => z.Name).ToDictionary(z => z.Name, z => new BonusKey(z.Bonus)));
    }

    public async Task<ApiResult<bool>> ReportBonusAsync(BonusReport report, ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(report.Server.Site, "/api/client/bonuses"));
        request.Headers.Add("X-API-Key", apiKey.Value);
        request.Content = JsonContent.Create(new ReportDto(report.Server.Name, report.Zone, report.Bonus.Value), options: Json);
        var result = await SendAsync<ReportResponseDto>(request, cancellationToken);
        return Map(result, r => r.Updated);
    }

    async Task<ApiResult<T>> GetAsync<T>(Server server, string list, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(server.Site, $"/api/servers/{server.Slug}/bonuses/{list}"));
        return await SendAsync<T>(request, cancellationToken);
    }

    async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var description = $"{request.Method} {request.RequestUri}";
        try
        {
            using var response = await this._httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return ApiResult<T>.Failure(OutcomeFor(response.StatusCode), $"{description} returned {(int)response.StatusCode}: {body}");
            }

            var envelope = JsonSerializer.Deserialize<EnvelopeDto<T>>(body, Json);
            if (envelope is not { Ok: true, Data: not null })
            {
                return ApiResult<T>.Failure(ApiOutcome.Unavailable, $"{description} returned an unexpected body: {body}");
            }

            return ApiResult<T>.Success(envelope.Data);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Failure(ApiOutcome.Unavailable, $"{description} failed: {exception.Message}");
        }
    }

    static ApiOutcome OutcomeFor(HttpStatusCode httpStatusCode) => httpStatusCode switch
    {
        HttpStatusCode.BadRequest => ApiOutcome.BadPayload,
        HttpStatusCode.Unauthorized => ApiOutcome.KeyRejected,
        HttpStatusCode.Forbidden => ApiOutcome.Banned,
        HttpStatusCode.NotFound => ApiOutcome.NotFound,
        _ => ApiOutcome.Unavailable,
    };

    static ApiResult<TOut> Map<TIn, TOut>(ApiResult<TIn> result, Func<TIn, TOut> map)
    {
        if (result.IsSuccess)
        {
            return ApiResult<TOut>.Success(map(result.Value!));
        }

        return ApiResult<TOut>.Failure(result.Outcome, result.Error!);
    }

    record EnvelopeDto<T>(bool Ok, T? Data);

    record TypeDto(string Key);

    record ZoneDto(string Name);

    record TodayDto(string Name, string Bonus);

    record ReportDto(string ServerName, string ZoneName, string BonusKey);

    record ReportResponseDto(bool Updated);
}
