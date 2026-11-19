using Microsoft.Win32;

namespace FZScout.Tests;

/// <summary>Uses a throwaway key under HKCU instead of the real settings key.</summary>
public class OnUnlessTurnedOffSettingTests : IDisposable
{
    readonly string _testKeyPath = $@"Software\FZScout.Tests.{Guid.NewGuid()}";
    readonly OnUnlessTurnedOffSetting _setting;

    public OnUnlessTurnedOffSettingTests()
    {
        this._setting = new OnUnlessTurnedOffSetting("TestSetting", this._testKeyPath);
    }

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

    [Fact]
    public void OnByDefault()
    {
        Assert.True(this._setting.IsOn);
    }

    [Fact]
    public void TurnOff()
    {
        this._setting.TurnOff();

        Assert.False(this._setting.IsOn);
    }

    [Fact]
    public void TurnOnAfterTurnOff()
    {
        this._setting.TurnOff();

        this._setting.TurnOn();

        Assert.True(this._setting.IsOn);
    }
}
