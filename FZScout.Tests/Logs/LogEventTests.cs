using FZScout.Logs;

namespace FZScout.Tests.Logs;

public class LogEventTests
{
    [Fact]
    public Task Parse()
    {
        // Real lines from an EverQuest log, with player names changed.
        var log = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Logs", "SampleLog.txt"));
        var events = log.Select(text => $"{LogEvent.Parse(text)} - {text}");

        return Verify(events);
    }

    [Fact]
    public void StripTimestamp()
    {
        Assert.Equal("You have slain a rat!", LogLine.StripTimestamp("[Mon Sep 14 16:10:29 2026] You have slain a rat!"));
    }

    [Fact]
    public void LineWithoutTimestampIsUnchanged()
    {
        Assert.Equal("You have slain a rat!", LogLine.StripTimestamp("You have slain a rat!"));
    }
}
