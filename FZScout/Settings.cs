using Microsoft.Win32;

namespace FZScout;

/// <summary>
/// The choices the user makes in the Settings window, other than the API key. 
/// </summary>
record Settings(bool LaunchesAtLogin, bool ShowsToastNotifications, bool DownloadsUpdatesAutomatically)
{
    const string DefaultSettingsKeyPath = @"Software\FZScout";
    const string ShowToastNotificationsValueName = "ShowToastNotifications";
    const string DownloadUpdatesAutomaticallyValueName = "DownloadUpdatesAutomatically";

    public static Settings Load(LaunchAtLogin.StartupApp launchAtLogin, string settingsKeyPath = DefaultSettingsKeyPath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(settingsKeyPath);
        return new Settings(
            LaunchesAtLogin: launchAtLogin.IsOn,
            ShowsToastNotifications: IsOnUnlessTurnedOff(key, ShowToastNotificationsValueName),
            DownloadsUpdatesAutomatically: IsOnUnlessTurnedOff(key, DownloadUpdatesAutomaticallyValueName));
    }

    public void Save(LaunchAtLogin.StartupApp launchAtLogin, string settingsKeyPath = DefaultSettingsKeyPath)
    {
        SaveLaunchAtLogin(launchAtLogin);
        using var key = Registry.CurrentUser.CreateSubKey(settingsKeyPath);
        SaveOnOrOff(key, ShowToastNotificationsValueName, ShowsToastNotifications);
        SaveOnOrOff(key, DownloadUpdatesAutomaticallyValueName, DownloadsUpdatesAutomatically);
    }

    static bool IsOnUnlessTurnedOff(RegistryKey? key, string valueName) => key?.GetValue(valueName) is not 0;

    static void SaveOnOrOff(RegistryKey key, string valueName, bool isOn)
    {
        key.SetValue(valueName, isOn ? 1 : 0, RegistryValueKind.DWord);
    }

    void SaveLaunchAtLogin(LaunchAtLogin.StartupApp launchAtLogin)
    {
        if (LaunchesAtLogin != launchAtLogin.IsOn)
        {
            if (LaunchesAtLogin)
            {
                launchAtLogin.TurnOn();
            }
            else
            {
                launchAtLogin.TurnOff();
            }
        }
    }
}
