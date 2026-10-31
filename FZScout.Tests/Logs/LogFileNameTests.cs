using FZScout.Logs;

namespace FZScout.Tests.Logs;

public class LogFileNameTests
{
    [Fact]
    public void Fangbreaker()
    {
        Assert.Equal(new Character("Bob", Server.Fangbreaker), LogFileName.TryParse(@"C:\EverQuest\Logs\eqlog_Bob_fangbreaker.txt"));
    }

    [Fact]
    public void ServerIgnoresCase()
    {
        Assert.Equal(new Character("Ann", Server.Frostreaver), LogFileName.TryParse(@"C:\EverQuest\Logs\eqlog_Ann_FrostReaver.txt"));
    }

    [Theory]
    [InlineData(@"C:\EverQuest\Logs\eqlog_Bob_teek.txt")]
    [InlineData(@"C:\EverQuest\Logs\eqlog_Bob_fangbreaker_old.txt")]
    [InlineData(@"C:\EverQuest\Logs\dbg.txt")]
    [InlineData(@"C:\EverQuest\Logs\eqlog_Bob.txt")]
    public void NotALogWeReport(string path)
    {
        Assert.Null(LogFileName.TryParse(path));
    }
}
