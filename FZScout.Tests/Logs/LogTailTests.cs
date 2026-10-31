using FZScout.Logs;

namespace FZScout.Tests.Logs;

public class LogTailTests : IDisposable
{
    readonly TempFolder _folder = new();
    readonly string _path;

    public LogTailTests()
    {
        this._path = this._folder.File("eqlog_Bob_fangbreaker.txt");
        File.WriteAllText(this._path, "[Mon Sep 14 16:10:29 2026] Old line\r\n");
    }

    public void Dispose() => this._folder.Dispose();

    [Fact]
    public void StartsAtTheEnd()
    {
        var tail = new LogTail(this._path);

        Assert.Empty(tail.ReadNewLines());
    }

    [Fact]
    public void FromStartReadsEverything()
    {
        var tail = new LogTail(this._path, fromStart: true);

        Assert.Equal(["[Mon Sep 14 16:10:29 2026] Old line"], tail.ReadNewLines());
    }

    [Fact]
    public void ReadsAddedLines()
    {
        var tail = new LogTail(this._path);

        File.AppendAllText(this._path, "first\r\nsecond\r\n");

        Assert.Equal(["first", "second"], tail.ReadNewLines());
        Assert.Empty(tail.ReadNewLines());
    }

    [Fact]
    public void PartialLineWaitsUntilItIsComplete()
    {
        var tail = new LogTail(this._path);

        File.AppendAllText(this._path, "You have ent");
        Assert.Empty(tail.ReadNewLines());

        File.AppendAllText(this._path, "ered Befallen.\r\n");
        Assert.Equal(["You have entered Befallen."], tail.ReadNewLines());
    }

    [Fact]
    public void ReadsWhileEverQuestHasTheFileOpenForWriting()
    {
        var tail = new LogTail(this._path);
        using var writer = new StreamWriter(new FileStream(this._path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));

        writer.Write("still open\r\n");
        writer.Flush();

        Assert.Equal(["still open"], tail.ReadNewLines());
    }

    [Fact]
    public void ReplacedFileIsReadFromItsStart()
    {
        var tail = new LogTail(this._path);

        File.WriteAllText(this._path, "new\r\n");

        Assert.Equal(["new"], tail.ReadNewLines());
    }

    [Fact]
    public void MissingFileHasNoLines()
    {
        var tail = new LogTail(this._folder.File("eqlog_Nobody_fangbreaker.txt"));

        Assert.Empty(tail.ReadNewLines());
    }
}
