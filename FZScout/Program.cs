using System.Reflection;
using FZScout.Site;
using Velopack;

namespace FZScout;

static class Program
{
    static async Task Main()
    {
        // Must run before anything else. vpk's packaging-time check for this flags it as misplaced because
        // Main is compiled into an async state machine, but it genuinely does run first.
        VelopackApp.Build().Run();

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
            new GitHubUpdateChecker()));
    }

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
