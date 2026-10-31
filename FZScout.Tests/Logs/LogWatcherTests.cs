using FZScout.Logs;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests.Logs;

public class LogWatcherTests : IDisposable
{
    const string Befallen = "[Thu Oct 29 12:00:00 2026] You have entered Befallen.\r\n";

    static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    readonly TempFolder _everQuest = new();
    readonly StringWriter _output = new();
    readonly LogWatcher _watcher;
    readonly EqProcess _eq;

    public LogWatcherTests()
    {
        this._watcher = new LogWatcher(new ConsoleReporter(this._output, new FakeTimeProvider(Start)));
        Directory.CreateDirectory(LogsFolder);
        this._eq = new EqProcess(100, this._everQuest.File("eqgame.exe"));
    }

    string LogsFolder => Path.Combine(this._everQuest.Path, "Logs");

    public void Dispose() => this._everQuest.Dispose();

    [Fact]
    public void ReportsEventsFromNewLines()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", "[Thu Oct 29 11:00:00 2026] You have entered Crushbone.\r\n");
        this._watcher.Scan([this._eq], Start);

        WriteLog("eqlog_Bob_fangbreaker.txt", $"{Befallen}[Thu Oct 29 12:00:01 2026] Some chatter\r\n");

        Assert.Equal(
            [new CharacterLogEvent(new Character("Bob", Server.Fangbreaker), new ZoneEnteredLogEvent("Befallen"))],
            this._watcher.Poll());
    }

    [Fact]
    public void ALogIsActiveOnceItGrows()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        WriteLog("eqlog_Ann_frostreaver.txt", Befallen);
        this._watcher.Scan([this._eq], Start);
        this._watcher.Poll();
        Assert.Empty(this._watcher.ActiveLogs);

        WriteLog("eqlog_Ann_frostreaver.txt", Befallen);
        this._watcher.Poll();

        var ann = Path.Combine(LogsFolder, "eqlog_Ann_frostreaver.txt");
        Assert.Equal([ann], this._watcher.ActiveLogs);
        Assert.Equal($"12:00:00 Watching {ann}", Output());
    }

    [Fact]
    public void LogsForOtherServersAreIgnored()
    {
        WriteLog("eqlog_Bob_teek.txt", string.Empty);
        this._watcher.Scan([this._eq], Start);

        WriteLog("eqlog_Bob_teek.txt", Befallen);

        Assert.Empty(this._watcher.Poll());
        Assert.Empty(this._watcher.ActiveLogs);
    }

    [Fact]
    public void ALogCreatedWhileWatchingIsReadFromItsStart()
    {
        this._watcher.Scan([this._eq], Start);

        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Scan([this._eq], Start.AddSeconds(30));

        Assert.Single(this._watcher.Poll());
    }

    [Fact]
    public void SeveralProcessesInOneFolderShareOneTail()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        this._watcher.Scan([this._eq, this._eq with { Id = 101 }], Start);

        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);

        Assert.Single(this._watcher.Poll());
    }

    [Fact]
    public void StopsWatchingWhenEverQuestExits()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        this._watcher.Scan([this._eq], Start);
        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Poll();

        this._watcher.Scan([], Start.AddSeconds(30));
        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);

        Assert.Empty(this._watcher.Poll());
        Assert.Empty(this._watcher.ActiveLogs);
        Assert.EndsWith($"Stopped watching {Path.Combine(LogsFolder, "eqlog_Bob_fangbreaker.txt")}", Output());
    }

    [Fact]
    public void ScanSaysWhoseLogsStopped()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        WriteLog("eqlog_Bob_teek.txt", string.Empty);
        Assert.Empty(this._watcher.Scan([this._eq], Start));

        Assert.Equal([new Character("Bob", Server.Fangbreaker)], this._watcher.Scan([], Start.AddSeconds(30)));
    }

    [Fact]
    public void WatchedLogsIncludeOnesThatHaventGrown()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        this._watcher.Scan([this._eq], Start);

        Assert.Equal([Path.Combine(LogsFolder, "eqlog_Bob_fangbreaker.txt")], this._watcher.WatchedLogs);
    }

    [Fact]
    public void WhenEverQuestStartsAgainItStartsAtTheEnd()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", string.Empty);
        this._watcher.Scan([this._eq], Start);
        this._watcher.Scan([], Start.AddSeconds(30));
        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);

        this._watcher.Scan([this._eq with { Id = 200 }], Start.AddSeconds(60));

        Assert.Empty(this._watcher.Poll());
    }

    [Fact]
    public void HintsToTurnOnLoggingWhenNoLogGrowsForFiveMinutes()
    {
        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Scan([this._eq], Start);

        this._watcher.Scan([this._eq], Start.AddSeconds(270));
        Assert.Equal(string.Empty, Output());

        this._watcher.Scan([this._eq], Start.AddSeconds(300));
        Assert.Equal($"12:00:00 WARNING: {LogWatcher.EverquestLogNotGrowingMessage}", Output());
    }

    [Fact]
    public void HintsOnlyOncePerProcess()
    {
        this._watcher.Scan([this._eq], Start);
        this._watcher.Scan([this._eq], Start.AddMinutes(5));
        this._watcher.Scan([this._eq], Start.AddMinutes(10));

        Assert.Single(Output().Split('\n'));
    }

    [Fact]
    public void AnotherProcessGetsItsOwnHint()
    {
        this._watcher.Scan([this._eq], Start);
        this._watcher.Scan([this._eq], Start.AddMinutes(5));
        this._watcher.Scan([this._eq with { Id = 200 }], Start.AddMinutes(6));
        this._watcher.Scan([this._eq with { Id = 200 }], Start.AddMinutes(11));

        Assert.Equal(2, Output().Split('\n').Length);
    }

    [Fact]
    public void NoHintWhenAnyLogGrows()
    {
        WriteLog("eqlog_Bob_teek.txt", Befallen);
        this._watcher.Scan([this._eq], Start);

        WriteLog("eqlog_Bob_teek.txt", Befallen);
        this._watcher.Scan([this._eq], Start.AddSeconds(30));
        this._watcher.Scan([this._eq], Start.AddMinutes(10));

        Assert.Equal(string.Empty, Output());
    }

    [Fact]
    public void NoHintWhenANewLogAppears()
    {
        this._watcher.Scan([this._eq], Start);

        WriteLog("eqlog_Bob_fangbreaker.txt", Befallen);
        this._watcher.Scan([this._eq], Start.AddMinutes(10));

        Assert.Equal(string.Empty, Output());
    }

    [Fact]
    public void MissingLogsFolderIsFine()
    {
        Directory.Delete(LogsFolder);

        this._watcher.Scan([this._eq], Start);

        Assert.Empty(this._watcher.Poll());
    }

    [Fact]
    public void FindingEverQuestProcessesDoesNotThrow()
    {
        Assert.NotNull(EqProcesses.Find());
    }

    void WriteLog(string name, string text) => File.AppendAllText(Path.Combine(LogsFolder, name), text);

    string Output() => this._output.ToString().TrimEnd();
}
