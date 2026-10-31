using System.Reflection;
using FZScout.Logs;
using FZScout.Site;

namespace FZScout;

static class Program
{
    static async Task Main()
    {
        var app = CreateAppWithRealDependencies();
        await app.RunAsync();
    }

    internal static App CreateAppWithRealDependencies()
    {
        var timeProvider = TimeProvider.System;
        return new App(new AppDependencies(
            new ConsoleUserInput(),
            GetVersion: GetVersion,
            timeProvider,
            new ConsoleReporter(timeProvider),
            TryClaimSingleInstance: SingleInstance.TryClaim,
            ApiKeyStore: new WindowsCredentialManagerApiKeyStore(),
            new FangbreakerZoneClient(),
            new GitHubUpdateChecker(),
            FindEqProcesses: EqProcesses.Find));
    }

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
