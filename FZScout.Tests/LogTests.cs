using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

public class LogTests
{
    readonly List<(string Line, LogLineSeverity Severity)> _lines = [];
    readonly Log _log;

    public LogTests()
    {
        this._log = new Log(new FakeTimeProvider(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero)));
        this._log.LineWritten += (line, severity) => this._lines.Add((line, severity));
    }

    [Fact]
    public void InfoIsInfo()
    {
        this._log.Info("Watching a log");

        Assert.Equal([("12:00:00 Watching a log", LogLineSeverity.Info)], this._lines);
    }

    [Fact]
    public void SuccessIsSuccess()
    {
        this._log.Success("Reported a bonus");

        Assert.Equal([("12:00:00 Reported a bonus", LogLineSeverity.Success)], this._lines);
    }

    [Fact]
    public void WarningIsWarning()
    {
        this._log.Warning("Hmm");

        Assert.Equal([("12:00:00 WARNING: Hmm", LogLineSeverity.Warning)], this._lines);
    }

    [Fact]
    public void ErrorIsError()
    {
        this._log.Error("Broken");

        Assert.Equal([("12:00:00 ERROR: Broken", LogLineSeverity.Error)], this._lines);
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
