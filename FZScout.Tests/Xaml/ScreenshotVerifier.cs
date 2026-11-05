using System.Runtime.CompilerServices;
using System.Windows;

namespace FZScout.Tests.Xaml;

/// <summary>
/// Fails if any binding couldn't be resolved while drawing,
/// then compares a screenshot to the verified PNG named after the calling test class and method.
/// </summary>
static class ScreenshotVerifier
{
    public static Task VerifyScreenshot(Func<Window> createBoundWindow, [CallerFilePath] string sourceFile = "") => VerifyPng(() => Screenshot.TakePng(createBoundWindow()), sourceFile);

    /// <summary>Takes the screenshot on the WPF thread.</summary>
    public static async Task VerifyPng(Func<byte[]> takePngOnWpfThread, [CallerFilePath] string sourceFile = "")
    {
        var (png, bindingErrors) = WpfThread.Invoke(() => BindingErrorRecorder.RecordBindingErrorsWhile(takePngOnWpfThread));
        Assert.True(bindingErrors.Count == 0, string.Join(Environment.NewLine + Environment.NewLine, bindingErrors));
        await Verifier.Verify(new MemoryStream(png), "png", sourceFile: sourceFile);
    }
}
