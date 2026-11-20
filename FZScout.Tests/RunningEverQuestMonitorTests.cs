using EverQuestLogWatcher;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests;

public class RunningEverQuestMonitorTests
{
    readonly StringWriter _output = new();
    readonly RunningEverQuestMonitor _monitor;
    IReadOnlyList<RunningEverQuestInstall> _runningEverQuestInstalls = [];

    public RunningEverQuestMonitorTests()
    {
        var time = new FakeTimeProvider();
        this._monitor = new RunningEverQuestMonitor(() => this._runningEverQuestInstalls, new Log(this._output, time), time);
    }

    string Output => this._output.ToString();

    [Fact]
    public void WarnsOnceWhenEverQuestLoggingIsOff()
    {
        var changes = new List<bool>();
        this._monitor.IsEverQuestLoggingEnabledChanged += changes.Add;
        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: false)];

        this._monitor.CheckRunningEverQuests();
        this._monitor.CheckRunningEverQuests();

        Assert.Equal([false], changes);
        Assert.Single(Output.Split('\n'), line => line.Contains(@"WARNING: EverQuest logging is off in C:\EverQuest"));
    }

    [Fact]
    public void SaysWhenEverQuestLoggingIsBackOn()
    {
        var changes = new List<bool>();
        this._monitor.IsEverQuestLoggingEnabledChanged += changes.Add;
        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: false)];
        this._monitor.CheckRunningEverQuests();

        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: true)];
        this._monitor.CheckRunningEverQuests();

        Assert.Equal([false, true], changes);
        Assert.Contains("EverQuest logging is on.", Output);
    }

    [Fact]
    public void SaysNothingWhenEverQuestLoggingIsOn()
    {
        var changes = new List<bool>();
        this._monitor.IsEverQuestLoggingEnabledChanged += changes.Add;
        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: true)];

        this._monitor.CheckRunningEverQuests();

        Assert.Empty(changes);
        Assert.Empty(Output);
    }

    [Fact]
    public void SaysWhenEverQuestStartsAndStopsRunning()
    {
        var changes = new List<bool>();
        this._monitor.IsEverQuestRunningChanged += changes.Add;

        this._monitor.CheckRunningEverQuests();
        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: true)];
        this._monitor.CheckRunningEverQuests();
        this._monitor.CheckRunningEverQuests();
        this._runningEverQuestInstalls = [];
        this._monitor.CheckRunningEverQuests();

        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void SkipsTheCheckWhileEverQuestHasEqclientIniLocked()
    {
        var changes = new List<bool>();
        var time = new FakeTimeProvider();
        var monitor = new RunningEverQuestMonitor(
            () => throw new IOException("The process cannot access the file because another process has locked a portion of the file."),
            new Log(this._output, time),
            time);
        monitor.IsEverQuestRunningChanged += changes.Add;

        monitor.CheckRunningEverQuests();

        Assert.Empty(changes);
        Assert.Empty(Output);
    }
}
