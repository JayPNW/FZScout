using System.Net;
using FZScout.Tests.Site;

namespace FZScout.Tests;

public class GitHubUpdateCheckerTests
{
    [Theory]
    [InlineData("2", "3", true)]
    [InlineData("3", "3", false)]
    [InlineData("4", "3", false)]
    [InlineData("3-ci.99", "3", true)]
    [InlineData("3-ci.99", "4", true)]
    [InlineData("3-ci.99+abc123", "3", true)]
    [InlineData("3-ci.99", "2", false)]
    public async Task AnnouncesNewerRelease(string current, string latest, bool announced)
    {
        var output = new StringWriter();
        var handler = new StubHttpHandler(HttpStatusCode.OK, $$"""{"tag_name":"v{{latest}}","html_url":"https://example.test/r"}""");
        var checker = new GitHubUpdateChecker(handler);

        await checker.CheckAsync(current, new ConsoleReporter(output, TimeProvider.System), TestContext.Current.CancellationToken);

        Assert.Equal(announced, output.ToString().Contains("https://example.test/r"));
    }

    [Fact]
    public async Task StaysQuietWhenGitHubIsUnreachable()
    {
        var output = new StringWriter();
        var checker = new GitHubUpdateChecker(new StubHttpHandler(new HttpRequestException()));

        await checker.CheckAsync("1", new ConsoleReporter(output, TimeProvider.System), TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, output.ToString());
    }
}
