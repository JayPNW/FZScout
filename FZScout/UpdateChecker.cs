using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FZScout;

/// <summary>
/// Releases are numbered 1, 2, 3... and a CI build of release N is <c>N-ci.99</c>, which comes before release N.
/// Asks GitHub for the latest release (never a prerelease) and tells the user if it is newer than this build.
/// </summary>
class UpdateChecker(ConsoleLog log, HttpMessageHandler httpMessageHandler) : IDisposable
{
    readonly HttpClient _githubApiHttpClient = new GitHubApiHttpClient(httpMessageHandler);

    public void Dispose()
    {
        this._githubApiHttpClient.Dispose();
    }

    public async Task CheckAsync(string? currentVersion, CancellationToken cancellationToken = default)
    {
        if (!TryParseVersion(currentVersion, out var current, out var isPrerelease))
        {
            return;
        }

        try
        {
            var latestReleaseApi = new Uri($"/repos/{GitHubRepository.OwnerAndName}/releases/latest", UriKind.Relative);

            using var request = new HttpRequestMessage(HttpMethod.Get, latestReleaseApi);
            using var response = await this._githubApiHttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var release = await response.Content.ReadFromJsonAsync<ReleaseDto>(cancellationToken);
            if (release is null || !TryParseVersion(release.TagName, out var latest, out _))
            {
                return;
            }

            // A prerelease of N comes before release N.
            var isNewer = isPrerelease ? latest >= current : latest > current;
            if (isNewer)
            {
                log.Success($"v{latest} is available: {release.HtmlUrl}");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException && !cancellationToken.IsCancellationRequested)
        {
            // Not being able to check is not worth bothering anyone about.
        }
    }

    /// <summary>Reads <c>1</c>, <c>v2</c>, <c>3-ci.99</c> or <c>3-ci.99+abc123</c>.</summary>
    public static bool TryParseVersion(string? text, out int number, out bool isPrerelease)
    {
        number = 0;
        isPrerelease = false;
        if (text is null)
        {
            return false;
        }

        var core = text.TrimStart('v').Split('+')[0];
        var parts = core.Split('-', 2);
        isPrerelease = parts.Length > 1;
        return int.TryParse(parts[0], out number);
    }

    class GitHubApiHttpClient : HttpClient
    {
        public GitHubApiHttpClient(HttpMessageHandler httpMessageHandler)
            : base(httpMessageHandler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.github.com");
            Timeout = TimeSpan.FromSeconds(30);
            DefaultRequestHeaders.UserAgent.ParseAdd("FZScout");
        }
    }

    record ReleaseDto(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl);
}
