using Microsoft.Win32;

namespace FZScout.Tests;

/// <summary>Uses throwaway keys under HKCU instead of the real Run and StartupApproved keys.</summary>
public class LaunchAtLoginTests : IDisposable
{
    readonly string _testKeyPath = $@"Software\FZScout.Tests.{Guid.NewGuid()}";
    readonly LaunchAtLogin _launchAtLogin;

    public LaunchAtLoginTests()
    {
        this._launchAtLogin = new LaunchAtLogin($@"{this._testKeyPath}\Run", $@"{this._testKeyPath}\StartupApproved");
    }

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

    [Fact]
    public void AddToStartupAppsRunsTheExeQuoted()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\Users\someone\AppData\Local\FZScout\current\FZScout.exe");

        Assert.Equal(@"""C:\Users\someone\AppData\Local\FZScout\current\FZScout.exe""", this._launchAtLogin.Command);
    }

    [Fact]
    public void RemoveFromStartupAppsRemovesIt()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");

        this._launchAtLogin.RemoveFromStartupApps();

        Assert.Null(this._launchAtLogin.Command);
    }

    [Fact]
    public void RemoveFromStartupAppsWhenNeverAddedDoesNothing()
    {
        this._launchAtLogin.RemoveFromStartupApps();

        Assert.Null(this._launchAtLogin.Command);
    }

    [Fact]
    public void AddingToStartupAppsTurnsItOn()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");

        Assert.True(this._launchAtLogin.IsOn);
    }

    [Fact]
    public void NeverAddedIsOff()
    {
        Assert.False(this._launchAtLogin.IsOn);
    }

    [Fact]
    public void TurnOffKeepsItInStartupApps()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");

        this._launchAtLogin.TurnOff();

        Assert.False(this._launchAtLogin.IsOn);
        Assert.NotNull(this._launchAtLogin.Command);
    }

    [Fact]
    public void AddingToStartupAppsAgainKeepsItOff()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");
        this._launchAtLogin.TurnOff();

        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");

        Assert.False(this._launchAtLogin.IsOn);
    }

    [Fact]
    public void TurnOnAfterTurnOff()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");
        this._launchAtLogin.TurnOff();

        this._launchAtLogin.TurnOn(@"C:\FZScout.exe");

        Assert.True(this._launchAtLogin.IsOn);
    }

    [Fact]
    public void TurnOnWhenNeverAdded()
    {
        this._launchAtLogin.TurnOn(@"C:\FZScout.exe");

        Assert.True(this._launchAtLogin.IsOn);
    }

    [Fact]
    public void ReaddingAfterRemovingWhileOffTurnsItOn()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");
        this._launchAtLogin.TurnOff();
        this._launchAtLogin.RemoveFromStartupApps();

        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");

        Assert.True(this._launchAtLogin.IsOn);
    }

    [Fact]
    public void TaskManagerTurnedOnIsOn()
    {
        this._launchAtLogin.AddToStartupApps(@"C:\FZScout.exe");
        using (var key = Registry.CurrentUser.CreateSubKey($@"{this._testKeyPath}\StartupApproved"))
        {
            key.SetValue("FZScout", new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
        }

        Assert.True(this._launchAtLogin.IsOn);
    }
}
