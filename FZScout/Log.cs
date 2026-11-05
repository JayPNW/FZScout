namespace FZScout;

/// <summary>The app's log: timestamped lines, each with a severity. Never pass it the API key.</summary>
class Log(TimeProvider timeProvider)
{
    /// <summary>Plain text, for tests.</summary>
    public Log(TextWriter writer, TimeProvider timeProvider)
        : this(timeProvider)
    {
        LineWritten += (line, _) => writer.WriteLine(line);
    }

    /// <summary>Raised with the whole line, timestamp and all, on whichever thread wrote it.</summary>
    public event Action<string, LogLineSeverity>? LineWritten;

    /// <summary>Raised with the message, without timestamp, whenever <see cref="Success"/> is written.</summary>
    public event Action<string>? SuccessWritten;

    /// <summary>Raised with the message, without timestamp or prefix, whenever <see cref="Error"/> is written.</summary>
    public event Action<string>? ErrorWritten;

    public void Info(string message) => Write(message, LogLineSeverity.Info);

    /// <summary>Something worked that the user cares about, like a bonus uploaded to the site.</summary>
    public void Success(string message)
    {
        Write(message, LogLineSeverity.Success);
        SuccessWritten?.Invoke(message);
    }

    public void Warning(string message) => Write($"WARNING: {message}", LogLineSeverity.Warning);

    public void Error(string message)
    {
        Write($"ERROR: {message}", LogLineSeverity.Error);
        ErrorWritten?.Invoke(message);
    }

    void Write(string message, LogLineSeverity severity)
    {
        LineWritten?.Invoke($"{timeProvider.GetLocalNow():HH:mm:ss} {message}", severity);
    }
}
