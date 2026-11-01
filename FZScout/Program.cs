using System.Reflection;
using FZScout.Logs;
using FZScout.Site;

namespace FZScout;

static class Program
{
    static async Task Main()
    {
        var app = CreateApp();
        await app.RunAsync();
    }

    internal static App CreateApp()
    {
        var timeProvider = TimeProvider.System;
        return new App(new AppDependencies(
            new ConsoleUserInput(),
            new ConsoleReporter(timeProvider),
            GetVersion: GetVersion,
            timeProvider,
            TryClaimSingleInstance: SingleInstance.TryClaim,
            ApiKeyStore: new WindowsCredentialManagerApiKeyStore(),
            new FangbreakerZoneClient(),
            new GitHubUpdateChecker(),
            FindEqProcesses: EqProcesses.Find));
    }

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
