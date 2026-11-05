using System.Reflection;
using System.Windows;
using FZScout.Site;
using Velopack;

namespace FZScout;

static class Program
{
    extension(VelopackApp)
    {
        public static void InstallPendingUpdates()
        {
            VelopackApp.Build()
                .OnAfterInstallFastCallback(_ => new LaunchAtLogin().AddToStartupApps(Environment.ProcessPath!))
                .OnBeforeUninstallFastCallback(_ => new LaunchAtLogin().RemoveFromStartupApps())
                .Run();
        }
    }


    [STAThread]
    static void Main()
    {
        VelopackApp.InstallPendingUpdates();
        using var updates = new WatchForNewUpdates(TimeProvider.System);

        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var userInput = new MessageBoxUserInput();
        using var trayApplication = new TrayApplication(CreateApp(userInput), userInput, updates.UpdateChecker);
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
