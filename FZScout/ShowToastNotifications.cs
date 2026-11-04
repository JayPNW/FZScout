using Microsoft.Win32;

namespace FZScout;

/// <summary>Whether FZScout shows toast notifications. On unless the user turned it off in Settings.</summary>
class ShowToastNotifications(string settingsKeyPath = @"Software\FZScout")
{
    const string ValueName = "ShowToastNotifications";

    public bool IsOn
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(settingsKeyPath);
            return key?.GetValue(ValueName) is not 0;
        }
    }

    public void TurnOn() => Save(1);

    public void TurnOff() => Save(0);

    void Save(int value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(settingsKeyPath);
        key.SetValue(ValueName, value, RegistryValueKind.DWord);
    }
}
