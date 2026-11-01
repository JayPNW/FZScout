using FZScout.Logs;
using FZScout.Site;

namespace FZScout;

record AppDependencies(
    IUserInput UserInput,
    ConsoleReporter Log,
    Func<string?> GetVersion,
    TimeProvider TimeProvider,
    Func<IDisposable?> TryClaimSingleInstance,
    IApiKeyStore ApiKeyStore,
    FangbreakerZoneClient FangbreakerZoneClient,
    GitHubUpdateChecker GitHubUpdateChecker,
    Func<IReadOnlyCollection<EqProcess>> FindEqProcesses);
