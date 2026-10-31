namespace FZScout;

/// <summary>Reads a line from the console, echoing * for each character so a secret like the API key doesn't end up on screen.</summary>
static class HiddenConsoleInput
{
    /// <summary>Reads on a background thread so the caller can keep working while the user types.</summary>
    public static Task<string> ReadLineInBackgroundAsync(CancellationToken cancellationToken) => Task.Run(ReadLine, cancellationToken);

    static string ReadLine()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        var text = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return text.ToString();
            }

            if (key.Key == ConsoleKey.Backspace && text.Length > 0)
            {
                text.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                text.Append(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}
