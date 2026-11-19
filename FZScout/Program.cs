using System.Reflection;
using System.Windows;
using FZScout.Site;
using Velopack;

namespace FZScout;

static class Program
{
    static readonly LaunchAtLogin.StartupApp StartupApp = new("FZScout");

    extension(VelopackApp)
    {
        public static void InstallPendingUpdates()
        {
            VelopackApp.Build()
                .OnAfterInstallFastCallback(_ => StartupApp.AddToStartupApps())
                .OnBeforeUninstallFastCallback(_ => StartupApp.RemoveFromStartupApps())
                .Run();
        }
    }

    [STAThread]
    static void Main()
    {
        VelopackApp.InstallPendingUpdates();
        using var updates = new WatchForNewUpdates(() => OnUnlessTurnedOffSetting.DownloadUpdatesAutomatically().IsOn, TimeProvider.System);

        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var userInput = new MessageBoxUserInput();
        using var trayApplication = new TrayApplication(CreateApp(userInput), userInput, updates.UpdateChecker, StartupApp);
        application.Startup += (_, _) => trayApplication.Start();
        application.Run();
    }

    internal static App CreateApp(IUserInput userInput)
    {
        var timeProvider = TimeProvider.System;
        return new App(new AppDependencies(
            userInput,
            new Log(timeProvider),
            GetVersion: GetVersion,
            timeProvider,
            TryClaimSingleInstance: SingleInstance.TryClaim,
            ApiKeyStore: new WindowsCredentialManagerApiKeyStore(),
            new FangbreakerZoneClient()));
    }

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
