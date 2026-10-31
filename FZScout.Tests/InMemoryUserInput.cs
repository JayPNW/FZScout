namespace FZScout.Tests;

/// <summary>
/// A user who asks to exit when <paramref name="exitRequested"/> is cancelled, presses a key whenever asked,
/// and types <paramref name="typedApiKey"/> right away whenever asked for an API key.
/// </summary>
class InMemoryUserInput(CancellationToken exitRequested, string typedApiKey) : IUserInput
{
    public CancellationToken ExitRequested => exitRequested;

    public int TimesWaitedForAnyKey { get; private set; }

    public int TimesAskedForApiKey { get; private set; }

    public void WaitForAnyKey() => TimesWaitedForAnyKey++;

    public Task<string> ReadApiKeyAsync(CancellationToken cancellationToken)
    {
        TimesAskedForApiKey++;
        return Task.FromResult(typedApiKey);
    }
}
