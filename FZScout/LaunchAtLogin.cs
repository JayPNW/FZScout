using Microsoft.Win32;

namespace FZScout;

/// <summary>
/// Launches FZScout when the user logs in to Windows. The user turns it on or off in Settings or in Task Manager's Startup apps,
/// which both keep that choice in the StartupApproved key, so reinstalling or updating never overrides it.
/// </summary>
class LaunchAtLogin(
    string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
    string startupApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run")
{
    const string ValueName = "FZScout";

    /// <summary>Task Manager marks an app turned off with an odd first byte, followed by when it was turned off.</summary>
    const byte TurnedOffFlag = 0x01;

    public string? Command
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
            return key?.GetValue(ValueName) as string;
        }
    }

    public bool IsOn => Command is not null && !IsTurnedOffInStartupApproved();

    /// <summary>Doesn't change whether the user turned it off.</summary>
    public void AddToStartupApps(string exePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(runKeyPath);
        key.SetValue(ValueName, $"\"{exePath}\"");
    }

    public void RemoveFromStartupApps()
    {
        using (var runKey = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true))
        {
            runKey?.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        ForgetWhetherTurnedOff();
    }

    public void TurnOn(string exePath)
    {
        AddToStartupApps(exePath);
        ForgetWhetherTurnedOff();
    }

    public void TurnOff()
    {
        var turnedOffMarker = new byte[12];
        turnedOffMarker[0] = 0x03;
        BitConverter.GetBytes(DateTime.UtcNow.ToFileTimeUtc()).CopyTo(turnedOffMarker, 4);
        using var key = Registry.CurrentUser.CreateSubKey(startupApprovedKeyPath);
        key.SetValue(ValueName, turnedOffMarker, RegistryValueKind.Binary);
    }

    bool IsTurnedOffInStartupApproved()
    {
        using var key = Registry.CurrentUser.OpenSubKey(startupApprovedKeyPath);
        return key?.GetValue(ValueName) is byte[] { Length: > 0 } marker && (marker[0] & TurnedOffFlag) != 0;
    }

    void ForgetWhetherTurnedOff()
    {
        using var key = Registry.CurrentUser.OpenSubKey(startupApprovedKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
