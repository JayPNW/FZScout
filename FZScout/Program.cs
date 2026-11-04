using System.Reflection;
using FZScout.Site;
using Velopack;

namespace FZScout;

static class Program
{
    [STAThread]
    static void Main()
    {
        InstallUpdates();
        using var updates = new WatchForNewUpdates(TimeProvider.System);

        ApplicationConfiguration.Initialize();
        using var window = new MainWindow();
        Application.Run(new TrayApplicationContext(window, CreateApp(window)));
    }

    /// <summary>Returns without doing anything on a normal launch, when Velopack hasn't started us to handle one of these.</summary>
    static void InstallUpdates()
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => new LaunchAtLogin().AddToStartupApps(Environment.ProcessPath!))
            .OnAfterUpdateFastCallback(_ => new LaunchAtLogin().AddToStartupApps(Environment.ProcessPath!))
            .OnBeforeUninstallFastCallback(_ => new LaunchAtLogin().RemoveFromStartupApps())
            .Run();
    }

    internal static App CreateApp(MainWindow window)
    {
        var timeProvider = TimeProvider.System;
        return new App(new AppDependencies(
            window,
            new Log(window.WriteLine, timeProvider),
            GetVersion: GetVersion,
            timeProvider,
            TryClaimSingleInstance: SingleInstance.TryClaim,
            ApiKeyStore: new WindowsCredentialManagerApiKeyStore(),
            new FangbreakerZoneClient()));
    }

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
