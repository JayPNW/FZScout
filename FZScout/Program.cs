using System.Reflection;
using System.Windows;
using FZScout.Site;
using LaunchAtLogin;
using Velopack;

namespace FZScout;

static class Program
{
    static readonly StartupApp StartupApp = new("FZScout");

    [STAThread]
    static void Main()
    {
        VelopackApp.Build()
            .KeepInStartupApps(StartupApp)
            .Run();
        using var updates = new WatchForNewUpdates(() => Settings.Load(StartupApp).UpdateAutomatically, TimeProvider.System);

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
            new FangbreakerZoneClient(),
            FindRunningEverQuestInstalls: new EverQuestLogWatcher.EverQuestLogWatcher().FindRunningInstalls));
    }

    static string? GetVersion() => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
