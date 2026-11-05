using FZScout.ViewModels;
using static FZScout.Tests.Xaml.ScreenshotVerifier;

namespace FZScout.Tests.Xaml;

public class MainWindowTests
{
    [Fact]
    public Task WithEachKindOfLogLine() => VerifyScreenshot(() =>
    {
        var viewModel = new AppViewModel("7.0.0", apiKey: "", launchesAtLogin: true, showsToastNotifications: true);
        viewModel.LogLines.Add(new LogLine("2026-11-05 12:00:00 FZScout v7.0.0", LogLineSeverity.Info));
        viewModel.LogLines.Add(new LogLine("2026-11-05 12:00:01 Reported a bonus. See https://fangbreaker.zone/bonuses", LogLineSeverity.Success));
        viewModel.LogLines.Add(new LogLine("2026-11-05 12:00:02 Couldn't tell which zone this is.", LogLineSeverity.Warning));
        viewModel.LogLines.Add(new LogLine("2026-11-05 12:00:03 ERROR: API key rejected. Enter a new one in Settings.", LogLineSeverity.Error));
        return new MainWindow(viewModel);
    });
}
