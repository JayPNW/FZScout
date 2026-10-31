using FZScout.Logs;
using FZScout.Site;

namespace FZScout;

record AppDependencies(
    IUserInput UserInput,
    Func<string?> GetVersion,
    TimeProvider TimeProvider,
    ConsoleReporter Log,
    Func<IDisposable?> TryClaimSingleInstance,
    IApiKeyStore ApiKeyStore,
    FangbreakerZoneClient FangbreakerZoneClient,
    GitHubUpdateChecker GitHubUpdateChecker,
    Func<IReadOnlyCollection<EqProcess>> FindEqProcesses);
