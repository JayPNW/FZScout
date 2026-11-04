using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

public class LogTests
{
    readonly List<(string Line, ConsoleColor? Color)> _lines = [];
    readonly Log _log;

    public LogTests()
    {
        this._log = new Log((line, color) => this._lines.Add((line, color)), new FakeTimeProvider(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void InfoIsPlain()
    {
        this._log.Info("Watching a log");

        Assert.Equal([("12:00:00 Watching a log", null)], this._lines);
    }

    [Fact]
    public void SuccessIsCyan()
    {
        this._log.Success("Reported a bonus");

        Assert.Equal([("12:00:00 Reported a bonus", ConsoleColor.Cyan)], this._lines);
    }

    [Fact]
    public void WarningIsYellow()
    {
        this._log.Warning("Hmm");

        Assert.Equal([("12:00:00 WARNING: Hmm", ConsoleColor.Yellow)], this._lines);
    }

    [Fact]
    public void ErrorIsRed()
    {
        this._log.Error("Broken");

        Assert.Equal([("12:00:00 ERROR: Broken", ConsoleColor.Red)], this._lines);
    }

    [Fact]
    public void SuccessIsAnnounced()
    {
        var announced = new List<string>();
        this._log.SuccessWritten += announced.Add;

        this._log.Success("Reported a bonus");

        Assert.Equal(["Reported a bonus"], announced);
    }

    [Fact]
    public void ErrorIsAnnounced()
    {
        var announced = new List<string>();
        this._log.ErrorWritten += announced.Add;

        this._log.Error("Broken");

        Assert.Equal(["Broken"], announced);
    }

    [Fact]
    public void InfoAndWarningAreNotAnnounced()
    {
        var announced = new List<string>();
        this._log.SuccessWritten += announced.Add;
        this._log.ErrorWritten += announced.Add;

        this._log.Info("Watching a log");
        this._log.Warning("Hmm");

        Assert.Empty(announced);
    }
}
