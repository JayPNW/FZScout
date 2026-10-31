using FZScout.Logs;
using FZScout.Site;

namespace FZScout;

/// <summary>Everything the app needs from outside the process. Tests pass in-memory versions.</summary>
/// <param name="ExitRequested">Cancelled when the user asks the app to exit.</param>
/// <param name="TryClaimSingleInstance">Returns a claim held until disposed, or null if another FZScout already holds it.</param>
/// <param name="WaitForAnyKey">Blocks until the user presses a key.</param>
/// <param name="ReadApiKeyAsync">Waits for the user to type an API key, without blocking the caller.</param>
/// <param name="FindEqProcesses">The EverQuest processes running right now.</param>
record AppDependencies(
    CancellationToken ExitRequested,
    Func<string?> GetVersion,
    TimeProvider TimeProvider,
    ConsoleReporter Log,
    Func<IDisposable?> TryClaimSingleInstance,
    Action WaitForAnyKey,
    Func<CancellationToken, Task<string>> ReadApiKeyAsync,
    IApiKeyStore ApiKeyStore,
    FangbreakerZoneClient FangbreakerZoneClient,
    GitHubClient GitHubClient,
    Func<IReadOnlyCollection<EqProcess>> FindEqProcesses);
