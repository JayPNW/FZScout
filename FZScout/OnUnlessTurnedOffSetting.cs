using Microsoft.Win32;

namespace FZScout;

/// <summary>An on/off setting saved in the registry, which is on until the user turns it off in Settings.</summary>
class OnUnlessTurnedOffSetting(string valueName, string settingsKeyPath = @"Software\FZScout")
{
    public bool IsOn
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(settingsKeyPath);
            return key?.GetValue(valueName) is not 0;
        }
    }

    public static OnUnlessTurnedOffSetting ShowToastNotifications() => new("ShowToastNotifications");

    public static OnUnlessTurnedOffSetting DownloadUpdatesAutomatically() => new("DownloadUpdatesAutomatically");

    public void TurnOnOrOff(bool turnOn)
    {
        if (turnOn)
        {
            TurnOn();
            return;
        }

        TurnOff();
    }

    public void TurnOn() => Save(1);

    public void TurnOff() => Save(0);

    void Save(int value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(settingsKeyPath);
        key.SetValue(valueName, value, RegistryValueKind.DWord);
    }
}
