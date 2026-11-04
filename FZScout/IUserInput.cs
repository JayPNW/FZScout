using FZScout.Site;

namespace FZScout;

interface IUserInput
{
    CancellationToken ExitRequested { get; }

    /// <summary>Tells the user another FZScout is already running, and blocks until they've seen it.</summary>
    void InformUserAppIsAlreadyRunning();

    /// <returns>Null if they entered nothing.</returns>
    Task<ApiKey?> ReadApiKeyAsync(CancellationToken cancellationToken);
}
