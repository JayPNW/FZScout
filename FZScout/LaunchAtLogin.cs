using Microsoft.Win32;

namespace FZScout;

/// <summary>Launches FZScout when the user logs in to Windows. They can turn it off in Task Manager's Startup apps.</summary>
class LaunchAtLogin(string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run")
{
    const string ValueName = "FZScout";

    public string? Command
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
            return key?.GetValue(ValueName) as string;
        }
    }

    public void Enable(string exePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(runKeyPath);
        key.SetValue(ValueName, $"\"{exePath}\"");
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
