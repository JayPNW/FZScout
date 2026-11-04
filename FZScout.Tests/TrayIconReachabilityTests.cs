using Microsoft.Win32;

namespace FZScout.Tests;

/// <summary>Uses throwaway keys under HKCU instead of the real tray settings.</summary>
public class TrayIconReachabilityTests : IDisposable
{
    const string ExePath = @"C:\Users\someone\AppData\Local\FZScout\current\FZScout.exe";
    readonly string _testKeyPath = $@"Software\FZScout.Tests.{Guid.NewGuid()}";
    readonly TrayIconReachability _reachability;

    public TrayIconReachabilityTests()
    {
        this._reachability = new TrayIconReachability(this.NotifyIconSettingsPath, this.TrayNotifyPath);
    }

    string NotifyIconSettingsPath => $@"{this._testKeyPath}\NotifyIconSettings";

    string TrayNotifyPath => $@"{this._testKeyPath}\TrayNotify";

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

    [Fact]
    public void ReachableOnOlderWindowsWithoutTheseSettings()
    {
        Assert.True(this._reachability.CanUserReachTrayIcon(ExePath));
    }

    [Fact]
    public void UnreachableWhenHiddenIconsMenuIsHiddenAndIconIsNotRecordedYet()
    {
        HideHiddenIconsMenu();

        Assert.False(this._reachability.CanUserReachTrayIcon(ExePath));
    }

    [Fact]
    public void UnreachableWhenHiddenIconsMenuIsHiddenAndIconIsHidden()
    {
        HideHiddenIconsMenu();
        RecordIcon("1", ExePath, isPromoted: null);

        Assert.False(this._reachability.CanUserReachTrayIcon(ExePath));
    }

    [Fact]
    public void ReachableWhenIconIsShownOnTaskbar()
    {
        HideHiddenIconsMenu();
        RecordIcon("1", @"C:\Other\Other.exe", isPromoted: null);
        RecordIcon("2", ExePath.ToUpperInvariant(), isPromoted: 1);

        Assert.True(this._reachability.CanUserReachTrayIcon(ExePath));
    }

    [Fact]
    public void UnreachableWhenOnlyAnotherAppsIconIsShownOnTaskbar()
    {
        HideHiddenIconsMenu();
        RecordIcon("1", @"C:\Other\Other.exe", isPromoted: 1);

        Assert.False(this._reachability.CanUserReachTrayIcon(ExePath));
    }

    [Fact]
    public void ReachableWhenHiddenIconsMenuIsShown()
    {
        using var trayNotify = Registry.CurrentUser.CreateSubKey(this.TrayNotifyPath);
        trayNotify.SetValue("SystemTrayChevronVisibility", 1);
        RecordIcon("1", ExePath, isPromoted: 0);

        Assert.True(this._reachability.CanUserReachTrayIcon(ExePath));
    }

    void HideHiddenIconsMenu()
    {
        using var trayNotify = Registry.CurrentUser.CreateSubKey(this.TrayNotifyPath);
        trayNotify.SetValue("SystemTrayChevronVisibility", 0);
    }

    void RecordIcon(string iconName, string exePath, int? isPromoted)
    {
        using var icon = Registry.CurrentUser.CreateSubKey($@"{this.NotifyIconSettingsPath}\{iconName}");
        icon.SetValue("ExecutablePath", exePath);
        if (isPromoted is { } value)
        {
            icon.SetValue("IsPromoted", value);
        }
    }
}
