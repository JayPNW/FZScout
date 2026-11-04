using Microsoft.Extensions.Time.Testing;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;

namespace FZScout.Tests;

public class GitHubUpdateCheckerTests
{
    const string Sha1OfTheEmptyFileTheFakeDownloadWrites = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";
    const string Sha256OfTheEmptyFileTheFakeDownloadWrites = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";

    [Theory]
    [InlineData("2.0.0", "3.0.0", true)]
    [InlineData("3.0.0", "3.0.0", false)]
    [InlineData("4.0.0", "3.0.0", false)]
    [InlineData("3.0.0-ci.99", "3.0.0", true)]
    [InlineData("3.0.0-ci.99", "4.0.0", true)]
    [InlineData("3.0.0-ci.99", "2.0.0", false)]
    public async Task DownloadsNewerRelease(string current, string latest, bool downloaded)
    {
        using var installDir = new TempDirectory();
        var checker = CreateChecker(current, new FakeUpdateSource(FeedWithRelease(latest)), installDir);

        await checker.DownloadNewestRelease(TestContext.Current.CancellationToken);

        Assert.Equal(downloaded, checker.DownloadedVersionToInstallAfterExit is not null);
    }

    [Fact]
    public async Task InstallsTheNewestOfSeveralDownloads()
    {
        using var installDir = new TempDirectory();
        var source = new FakeUpdateSource(FeedWithRelease("2.0.0"));
        var checker = CreateChecker("1.0.0", source, installDir);
        await checker.DownloadNewestRelease(TestContext.Current.CancellationToken);

        source.Feed = FeedWithRelease("3.0.0");
        await checker.DownloadNewestRelease(TestContext.Current.CancellationToken);

        Assert.Equal(SemanticVersion.Parse("3.0.0"), checker.DownloadedVersionToInstallAfterExit);
    }

    [Fact]
    public async Task KeepsAnEarlierDownloadWhenGitHubBecomesUnreachable()
    {
        using var installDir = new TempDirectory();
        var source = new FakeUpdateSource(FeedWithRelease("2.0.0"));
        var checker = CreateChecker("1.0.0", source, installDir);
        await checker.DownloadNewestRelease(TestContext.Current.CancellationToken);

        source.ExceptionToThrowWhenChecked = new HttpRequestException();
        await checker.DownloadNewestRelease(TestContext.Current.CancellationToken);

        Assert.Equal(SemanticVersion.Parse("2.0.0"), checker.DownloadedVersionToInstallAfterExit);
    }

    [Fact]
    public async Task StopsQuietlyWhenCancelledBetweenChecks()
    {
        using var installDir = new TempDirectory();
        var checker = CreateChecker("1.0.0", new FakeUpdateSource(FeedWithRelease("2.0.0")), installDir);
        using var stopChecking = new CancellationTokenSource();
        var checking = checker.DownloadNewestReleaseNowAndDailyUntilCancelled(new FakeTimeProvider(), stopChecking.Token);

        stopChecking.Cancel();
        await checking;

        Assert.True(checking.IsCompletedSuccessfully);
    }

    [Theory]
    [InlineData("3.0.0", false)]
    [InlineData("3.0.0-ci.99", true)]
    [InlineData(null, false)]
    public void AcceptsPrereleasesOnlyWhenCurrentVersionIsPrerelease(string? current, bool acceptsPrereleases)
    {
        SemanticVersion? currentlyInstalledVersion = null;
        if (current is not null)
        {
            currentlyInstalledVersion = SemanticVersion.Parse(current);
        }

        var source = GitHubUpdateChecker.CreateGitHubSource(currentlyInstalledVersion);

        Assert.Equal(acceptsPrereleases, source.Prerelease);
    }

    [Fact]
    public async Task StaysQuietWhenGitHubIsUnreachable()
    {
        using var installDir = new TempDirectory();
        var checker = CreateChecker("1.0.0", new FakeUpdateSource(new VelopackAssetFeed(), new HttpRequestException()), installDir);

        await checker.DownloadNewestRelease(TestContext.Current.CancellationToken);

        Assert.Null(checker.DownloadedVersionToInstallAfterExit);
    }

    static GitHubUpdateChecker CreateChecker(string current, FakeUpdateSource source, TempDirectory installDir)
    {
        var locator = new TestVelopackLocator("FZScout", current, installDir.Path, NullVelopackLogger.Instance);
        return new GitHubUpdateChecker(source, locator);
    }

    static VelopackAssetFeed FeedWithRelease(string version) => new()
    {
        Assets =
        [
            new VelopackAsset
            {
                PackageId = "FZScout",
                Version = SemanticVersion.Parse(version),
                Type = VelopackAssetType.Full,
                FileName = $"FZScout-{version}-full.nupkg",
                SHA1 = Sha1OfTheEmptyFileTheFakeDownloadWrites,
                SHA256 = Sha256OfTheEmptyFileTheFakeDownloadWrites,
            },
        ],
    };
}
