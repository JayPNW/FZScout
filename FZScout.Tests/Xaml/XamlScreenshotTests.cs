using System.Runtime.CompilerServices;

namespace FZScout.Tests.Xaml;

/// <summary>
/// Each XAML file in FZScout gets a test class named after it, like AboutWindowTests for AboutWindow.xaml,
/// which binds it to a view model and verifies a screenshot of it.
/// </summary>
public class XamlScreenshotTests
{
    static string TestsDirectory { get; } = Path.GetDirectoryName(ThisFilePath())!;

    static string AppProjectDirectory { get; } = Path.GetFullPath(Path.Combine(TestsDirectory, "..", "..", "FZScout"));

    [Fact]
    public void EveryXamlFileHasATestClassWithAScreenshot()
    {
        var xamlFilesWithoutScreenshots = Directory
            .EnumerateFiles(AppProjectDirectory, "*.xaml", SearchOption.AllDirectories)
            .Where(xamlFile => !IsBuildOutput(xamlFile))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(xamlName => !HasVerifiedScreenshot(xamlName!))
            .ToList();

        Assert.Empty(xamlFilesWithoutScreenshots);
    }

    static bool HasVerifiedScreenshot(string xamlName) => Directory.EnumerateFiles(TestsDirectory, $"{xamlName}Tests.*.verified.png").Any();

    static bool IsBuildOutput(string path)
    {
        var relativePath = Path.GetRelativePath(AppProjectDirectory, path);
        return relativePath.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    static string ThisFilePath([CallerFilePath] string path = "") => path;
}
