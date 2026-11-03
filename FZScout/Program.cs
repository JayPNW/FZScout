using System.Reflection;
using FZScout.Site;
using Velopack;

namespace FZScout;

static class Program
{
    static async Task Main()
    {
        VelopackApp.Build().Run();
        using var pendingUpdate = await new GitHubUpdateChecker().DownloadLatestAndApplyOnDispose();

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
            new FangbreakerZoneClient()));
    }

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
