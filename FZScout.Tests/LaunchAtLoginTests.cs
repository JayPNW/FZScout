using Microsoft.Win32;

namespace FZScout.Tests;

/// <summary>Uses a throwaway key under HKCU instead of the real Run key.</summary>
public class LaunchAtLoginTests : IDisposable
{
    readonly string _runKeyPath = $@"Software\FZScout.Tests.{Guid.NewGuid()}";
    readonly LaunchAtLogin _launchAtLogin;

    public LaunchAtLoginTests()
    {
        this._launchAtLogin = new LaunchAtLogin(this._runKeyPath);
    }

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(this._runKeyPath, throwOnMissingSubKey: false);

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
}
