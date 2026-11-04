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
        var feed = new VelopackAssetFeed
        {
            Assets =
            [
                new VelopackAsset
                {
                    PackageId = "FZScout",
                    Version = SemanticVersion.Parse(latest),
                    Type = VelopackAssetType.Full,
                    FileName = "FZScout-win-Full.nupkg",
                    SHA1 = Sha1OfTheEmptyFileTheFakeDownloadWrites,
                    SHA256 = Sha256OfTheEmptyFileTheFakeDownloadWrites,
                },
            ],
        };
        using var installDir = new TempDirectory();
        var locator = new TestVelopackLocator("FZScout", current, installDir.Path, NullVelopackLogger.Instance);
        var checker = new GitHubUpdateChecker(new FakeUpdateSource(feed), locator);

        var pendingUpdate = await checker.DownloadLatestAndApplyOnDispose(TestContext.Current.CancellationToken);

        Assert.Equal(downloaded, pendingUpdate is not null);
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
        var locator = new TestVelopackLocator("FZScout", "1.0.0", installDir.Path, NullVelopackLogger.Instance);
        var checker = new GitHubUpdateChecker(new FakeUpdateSource(new VelopackAssetFeed(), new HttpRequestException()), locator);

        var pendingUpdate = await checker.DownloadLatestAndApplyOnDispose(TestContext.Current.CancellationToken);

        Assert.Null(pendingUpdate);
    }
}
