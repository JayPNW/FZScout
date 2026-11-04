using FZScout.Site;

namespace FZScout;

record AppDependencies(
    IUserInput UserInput,
    Log Log,
    Func<string?> GetVersion,
    TimeProvider TimeProvider,
    Func<IDisposable?> TryClaimSingleInstance,
    IApiKeyStore ApiKeyStore,
    FangbreakerZoneClient FangbreakerZoneClient);
