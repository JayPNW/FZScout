using Microsoft.Win32;

namespace FZScout.Tests;

/// <summary>Uses a throwaway key under HKCU instead of the real settings key.</summary>
public class ShowToastNotificationsTests : IDisposable
{
    readonly string _testKeyPath = $@"Software\FZScout.Tests.{Guid.NewGuid()}";
    readonly ShowToastNotifications _showToastNotifications;

    public ShowToastNotificationsTests()
    {
        this._showToastNotifications = new ShowToastNotifications(this._testKeyPath);
    }

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

    [Fact]
    public void OnByDefault()
    {
        Assert.True(this._showToastNotifications.IsOn);
    }

    [Fact]
    public void TurnOff()
    {
        this._showToastNotifications.TurnOff();

        Assert.False(this._showToastNotifications.IsOn);
    }

    [Fact]
    public void TurnOnAfterTurnOff()
    {
        this._showToastNotifications.TurnOff();

        this._showToastNotifications.TurnOn();

        Assert.True(this._showToastNotifications.IsOn);
    }
}
