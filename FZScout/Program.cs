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
            ExitRequested: CtrlC.CreateExitRequestedToken(),
            GetVersion: GetVersion,
            timeProvider,
            new ConsoleReporter(timeProvider),
            TryClaimSingleInstance: SingleInstance.TryClaim,
            WaitForAnyKey: WaitForAnyKey,
            ReadApiKeyAsync: HiddenConsoleInput.ReadLineInBackgroundAsync,
            ApiKeyStore: new WindowsCredentialManagerApiKeyStore(),
            new FangbreakerZoneClient(),
            new GitHubClient(),
            FindEqProcesses: EqProcesses.Find));
    }

    static void WaitForAnyKey() => Console.ReadKey(intercept: true);

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
