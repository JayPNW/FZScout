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
            .OnAfterInstallFastCallback(_ => new LaunchAtLogin().Enable(Environment.ProcessPath!))
            .OnAfterUpdateFastCallback(_ => new LaunchAtLogin().Enable(Environment.ProcessPath!))
            .OnBeforeUninstallFastCallback(_ => new LaunchAtLogin().Disable())
            .Run();
        var pendingUpdateDownload = Task.Run(() => new GitHubUpdateChecker().DownloadLatestAndApplyOnDispose());

        ApplicationConfiguration.Initialize();
        using (var window = new MainWindow())
        {
            Application.Run(new TrayApplicationContext(window, CreateApp(window)));
        }

        using var pendingUpdate = pendingUpdateDownload.GetAwaiter().GetResult();
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
