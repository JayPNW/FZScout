using Microsoft.Win32;

namespace FZScout.Tests;

/// <summary>Uses a throwaway key under HKCU instead of the real settings key, and a throwaway startup app that is never turned on.</summary>
public class SettingsTests : IDisposable
{
    readonly string _testKeyPath = $@"Software\FZScout.Tests.{Guid.NewGuid()}";
    readonly LaunchAtLogin.StartupApp _neverLaunchedStartupApp = new($"FZScout.Tests.{Guid.NewGuid()}");

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

    [Fact]
    public void OnByDefault()
    {
        var settings = Settings.Load(this._neverLaunchedStartupApp, this._testKeyPath);

        Assert.Equal(new Settings(LaunchesAtLogin: false, ShowsToastNotifications: true, UpdateAutomatically: true), settings);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void LoadsWhatWasSaved(bool showsToastNotifications, bool updateAutomatically)
    {
        var saved = new Settings(LaunchesAtLogin: false, showsToastNotifications, updateAutomatically);

        saved.Save(this._neverLaunchedStartupApp, this._testKeyPath);

        Assert.Equal(saved, Settings.Load(this._neverLaunchedStartupApp, this._testKeyPath));
    }
}
