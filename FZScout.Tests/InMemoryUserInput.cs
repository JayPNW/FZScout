using FZScout.Site;

namespace FZScout.Tests;

/// <summary>
/// A user who asks to exit when <paramref name="exitRequested"/> is cancelled, sees that the app is already running whenever told,
/// and types <see cref="TypedApiKey"/> right away whenever asked for an API key.
/// </summary>
class InMemoryUserInput(CancellationToken exitRequested, ApiKey? typedApiKey) : IUserInput
{
    public CancellationToken ExitRequested => exitRequested;

    /// <summary>Null means the user just presses Enter.</summary>
    public ApiKey? TypedApiKey { get; set; } = typedApiKey;

    public int TimesInformedAppIsAlreadyRunning { get; private set; }

    public int TimesAskedForApiKey { get; private set; }

    public void InformUserAppIsAlreadyRunning() => TimesInformedAppIsAlreadyRunning++;

    public Task<ApiKey?> ReadApiKeyAsync(CancellationToken cancellationToken)
    {
        TimesAskedForApiKey++;
        return Task.FromResult(TypedApiKey);
    }
}
