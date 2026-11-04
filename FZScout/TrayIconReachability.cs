using Microsoft.Win32;

namespace FZScout;

/// <summary>
/// Windows 11 can hide a tray icon, and can also hide the menu of hidden icons, leaving the user no way to reach the icon.
/// Both settings are in the registry.
/// </summary>
class TrayIconReachability(
    string notifyIconSettingsPath = @"Control Panel\NotifyIconSettings",
    string trayNotifyPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify")
{
    /// <summary>
    /// A new icon is hidden until the user chooses to show it, so an icon Windows hasn't recorded yet counts as hidden.
    /// Older Windows has no setting for the hidden icons menu and always shows it.
    /// </summary>
    public bool CanUserReachTrayIcon(string exePath) => IsTrayIconShownOnTaskbar(exePath) || IsHiddenIconsMenuShown();

    bool IsTrayIconShownOnTaskbar(string exePath)
    {
        using var settings = Registry.CurrentUser.OpenSubKey(notifyIconSettingsPath);
        if (settings is null)
        {
            return false;
        }

        foreach (var iconName in settings.GetSubKeyNames())
        {
            using var icon = settings.OpenSubKey(iconName);
            if (string.Equals(icon?.GetValue("ExecutablePath") as string, exePath, StringComparison.OrdinalIgnoreCase) && icon?.GetValue("IsPromoted") is 1)
            {
                return true;
            }
        }

        return false;
    }

    bool IsHiddenIconsMenuShown()
    {
        using var trayNotify = Registry.CurrentUser.OpenSubKey(trayNotifyPath);
        return trayNotify?.GetValue("SystemTrayChevronVisibility") is not 0;
    }
}
