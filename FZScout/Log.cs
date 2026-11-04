namespace FZScout;

/// <summary>The app's log: timestamped lines, some colored. Never pass it the API key.</summary>
class Log(Action<string, ConsoleColor?> write, TimeProvider timeProvider)
{
    /// <summary>Plain text, for tests.</summary>
    public Log(TextWriter writer, TimeProvider timeProvider)
        : this((line, _) => writer.WriteLine(line), timeProvider)
    {
    }

    /// <summary>Raised with the message, without timestamp, whenever <see cref="Success"/> is written.</summary>
    public event Action<string>? SuccessWritten;

    /// <summary>Raised with the message, without timestamp or prefix, whenever <see cref="Error"/> is written.</summary>
    public event Action<string>? ErrorWritten;

    public void Info(string message) => Write(message, null);

    /// <summary>Something worked that the user cares about, like a bonus uploaded to the site.</summary>
    public void Success(string message)
    {
        Write(message, ConsoleColor.Cyan);
        SuccessWritten?.Invoke(message);
    }

    public void Warning(string message) => Write($"WARNING: {message}", ConsoleColor.Yellow);

    public void Error(string message)
    {
        Write($"ERROR: {message}", ConsoleColor.Red);
        ErrorWritten?.Invoke(message);
    }

    void Write(string message, ConsoleColor? color)
    {
        write($"{timeProvider.GetLocalNow():HH:mm:ss} {message}", color);
    }
}
