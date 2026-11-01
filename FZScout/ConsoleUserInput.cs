using FZScout.Site;

namespace FZScout;

/// <summary>The real keyboard. Ctrl+C asks the app to exit, so it can stop cleanly instead of being killed.</summary>
class ConsoleUserInput : IUserInput
{
    readonly CancellationTokenSource _exitRequested = new();

    public ConsoleUserInput()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            this._exitRequested.Cancel();
        };
    }

    public CancellationToken ExitRequested => this._exitRequested.Token;

    /// <summary>Waits for a key so the window doesn't close before the user can read why.</summary>
    public void InformUserAppIsAlreadyRunning()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("FZScout is already running.");
        Console.ResetColor();
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey(intercept: true);
    }

    /// <summary>Reads on a background thread so the caller can keep working while the user types.</summary>
    public Task<ApiKey?> ReadApiKeyAsync(CancellationToken cancellationToken) => Task.Run(ReadApiKey, cancellationToken);

    static ApiKey? ReadApiKey()
    {
        var text = ReadLineHidden().Trim();
        if (text.Length == 0)
        {
            return null;
        }

        return new ApiKey(text);
    }

    /// <summary>Reads a line, echoing * for each character so the API key doesn't end up on screen.</summary>
    static string ReadLineHidden()
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
