namespace FZScout;

/// <summary>Turns Ctrl+C into a request to exit, so the app can stop cleanly instead of being killed.</summary>
static class CtrlC
{
    /// <returns>A token cancelled when the user presses Ctrl+C.</returns>
    public static CancellationToken CreateExitRequestedToken()
    {
        var exitRequested = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            exitRequested.Cancel();
        };

        return exitRequested.Token;
    }
}
