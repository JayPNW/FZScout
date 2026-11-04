using System.Reflection;
using FZScout.Site;
using Velopack;

namespace FZScout;

static class Program
{
    [STAThread]
    static void Main()
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => new LaunchAtLogin().AddToStartupApps(Environment.ProcessPath!))
            .OnAfterUpdateFastCallback(_ => new LaunchAtLogin().AddToStartupApps(Environment.ProcessPath!))
            .OnBeforeUninstallFastCallback(_ => new LaunchAtLogin().RemoveFromStartupApps())
            .Run();
        var updateChecker = new GitHubUpdateChecker();
        using var stopCheckingForUpdates = new CancellationTokenSource();
        var checkingForUpdates = Task.Run(() => updateChecker.DownloadNewestReleaseNowAndDailyUntilCancelled(TimeProvider.System, stopCheckingForUpdates.Token));

        ApplicationConfiguration.Initialize();
        using (var window = new MainWindow())
        {
            Application.Run(new TrayApplicationContext(window, CreateApp(window)));
        }

        stopCheckingForUpdates.Cancel();
        checkingForUpdates.GetAwaiter().GetResult();
        updateChecker.InstallDownloadedUpdateAfterExit();
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
