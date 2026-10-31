using System.Net.Http.Json;

namespace FZScout;

/// <summary>Talks to GitHub about FZScout's own repository.</summary>
class GitHubClient(HttpMessageHandler httpMessageHandler)
{
    readonly HttpClient _httpClient = new(httpMessageHandler) { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>Talks to the real GitHub.</summary>
    public GitHubClient()
        : this(new HttpClientHandler())
    {
    }

    /// <summary>The latest release, never a prerelease, or null if GitHub can't say right now.</summary>
    public async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{GitHubRepository.OwnerAndName}/releases/latest");
            request.Headers.UserAgent.ParseAdd("FZScout");
            using var response = await this._httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
