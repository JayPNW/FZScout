namespace FZScout;

/// <summary>The app's log: timestamped lines, colored on the console. Never pass it the API key.</summary>
/// <param name="write">Writes one line, in a color if one is given.</param>
class ConsoleLog(Action<string, ConsoleColor?> write, TimeProvider timeProvider)
{
    /// <summary>Plain text, for tests.</summary>
    public ConsoleLog(TextWriter writer, TimeProvider timeProvider)
        : this((line, _) => writer.WriteLine(line), timeProvider)
    {
    }

    public static ConsoleLog ForConsole(TimeProvider timeProvider) => new(WriteToConsole, timeProvider);

    public void Info(string message) => Write(message, null);

    /// <summary>Something worked that the user cares about, like a bonus uploaded to the site.</summary>
    public void Success(string message) => Write(message, ConsoleColor.Yellow);

    public void Warning(string message) => Write($"WARNING: {message}", ConsoleColor.Cyan);

    public void Error(string message) => Write($"ERROR: {message}", ConsoleColor.Red);

    static void WriteToConsole(string line, ConsoleColor? color)
    {
        if (color is null)
        {
            Console.WriteLine(line);
            return;
        }

        Console.ForegroundColor = color.Value;
        Console.WriteLine(line);
        Console.ResetColor();
    }

    void Write(string message, ConsoleColor? color)
    {
        write($"{timeProvider.GetLocalNow():HH:mm:ss} {message}", color);
    }
}
