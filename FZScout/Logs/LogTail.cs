using System.Text;

namespace FZScout.Logs;

class LogTail
{
    long _position;
    string _partialLine = string.Empty;

    /// <param name="fromStart">Read the whole file, instead of starting where it ends now.</param>
    public LogTail(string path, bool fromStart = false)
    {
        Path = path;
        if (!fromStart)
        {
            this._position = Length(path);
        }
    }

    public string Path { get; }

    /// <summary>The complete lines added since the last read. A line still being written waits for the next read.</summary>
    public IReadOnlyList<string> ReadNewLines()
    {
        var length = Length(Path);
        if (length < this._position)
        {
            // The file was replaced or truncated; what's there now is new.
            this._position = 0;
            this._partialLine = string.Empty;
        }

        if (length == this._position)
        {
            return [];
        }

        var text = this._partialLine + ReadTextFromPositionAndAdvance();

        var lines = text.Split('\n');
        this._partialLine = lines[^1];
        return [.. lines[..^1].Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0)];
    }

    string ReadTextFromPositionAndAdvance()
    {
        using var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        stream.Position = this._position;
        using var reader = new StreamReader(stream, Encoding.Latin1);
        var text = reader.ReadToEnd();
        this._position = stream.Position;
        return text;
    }

    static long Length(string path)
    {
        var file = new FileInfo(path);
        return file.Exists ? file.Length : 0;
    }
}
