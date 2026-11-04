namespace FZScout;

/// <summary>The app's log: timestamped lines, some colored. Never pass it the API key.</summary>
class Log(Action<string, ConsoleColor?> write, TimeProvider timeProvider)
{
    /// <summary>Plain text, for tests.</summary>
    public Log(TextWriter writer, TimeProvider timeProvider)
        : this((line, _) => writer.WriteLine(line), timeProvider)
    {
    }

    public void Info(string message) => Write(message, null);

    /// <summary>Something worked that the user cares about, like a bonus uploaded to the site.</summary>
    public void Success(string message) => Write(message, ConsoleColor.Cyan);

    public void Warning(string message) => Write($"WARNING: {message}", ConsoleColor.Yellow);

    public void Error(string message) => Write($"ERROR: {message}", ConsoleColor.Red);

    void Write(string message, ConsoleColor? color)
    {
        write($"{timeProvider.GetLocalNow():HH:mm:ss} {message}", color);
    }
}
