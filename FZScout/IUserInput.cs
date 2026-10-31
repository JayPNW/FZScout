namespace FZScout;

interface IUserInput
{
    /// <summary>Cancelled when the user asks the app to exit.</summary>
    CancellationToken ExitRequested { get; }

    /// <summary>Blocks until the user presses a key.</summary>
    void WaitForAnyKey();

    /// <summary>Waits for the user to type an API key, without blocking the caller.</summary>
    Task<string> ReadApiKeyAsync(CancellationToken cancellationToken);
}
