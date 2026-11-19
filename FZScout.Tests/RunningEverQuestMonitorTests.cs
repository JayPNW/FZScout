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

        this._monitor.CheckWhetherEverQuestLoggingIsEnabled();
        this._monitor.CheckWhetherEverQuestLoggingIsEnabled();

        Assert.Equal([false], changes);
        Assert.Single(Output.Split('\n'), line => line.Contains(@"WARNING: EverQuest logging is off in C:\EverQuest"));
    }

    [Fact]
    public void SaysWhenEverQuestLoggingIsBackOn()
    {
        var changes = new List<bool>();
        this._monitor.IsEverQuestLoggingEnabledChanged += changes.Add;
        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: false)];
        this._monitor.CheckWhetherEverQuestLoggingIsEnabled();

        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: true)];
        this._monitor.CheckWhetherEverQuestLoggingIsEnabled();

        Assert.Equal([false, true], changes);
        Assert.Contains("EverQuest logging is on.", Output);
    }

    [Fact]
    public void SaysNothingWhenEverQuestLoggingIsOn()
    {
        var changes = new List<bool>();
        this._monitor.IsEverQuestLoggingEnabledChanged += changes.Add;
        this._runningEverQuestInstalls = [new(@"C:\EverQuest", IsLoggingEnabled: true)];

        this._monitor.CheckWhetherEverQuestLoggingIsEnabled();

        Assert.Empty(changes);
        Assert.Empty(Output);
    }
}
