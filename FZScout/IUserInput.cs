using FZScout.Site;

namespace FZScout;

interface IUserInput
{
    /// <summary>Cancelled when the user asks the app to exit.</summary>
    CancellationToken ExitRequested { get; }

    /// <summary>Tells the user another FZScout is already running, and blocks until they've seen it.</summary>
    void InformUserAppIsAlreadyRunning();

    /// <summary>Waits for the user to type an API key, without blocking the caller. Null if they entered nothing.</summary>
    Task<ApiKey?> ReadApiKeyAsync(CancellationToken cancellationToken);
}
