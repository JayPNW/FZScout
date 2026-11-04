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
    public void EnableRunsTheExeQuoted()
    {
        this._launchAtLogin.Enable(@"C:\Users\someone\AppData\Local\FZScout\current\FZScout.exe");

        Assert.Equal(@"""C:\Users\someone\AppData\Local\FZScout\current\FZScout.exe""", this._launchAtLogin.Command);
    }

    [Fact]
    public void DisableRemovesIt()
    {
        this._launchAtLogin.Enable(@"C:\FZScout.exe");

        this._launchAtLogin.Disable();

        Assert.Null(this._launchAtLogin.Command);
    }

    [Fact]
    public void DisableWhenNeverEnabledDoesNothing()
    {
        this._launchAtLogin.Disable();

        Assert.Null(this._launchAtLogin.Command);
    }
}
