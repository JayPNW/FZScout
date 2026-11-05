using System.Windows;

namespace FZScout;

/// <summary>Remembers when the user asks to exit, and tells the user things in message boxes.</summary>
class MessageBoxUserInput : IUserInput, IDisposable
{
    readonly CancellationTokenSource _exitRequested = new();

    public CancellationToken ExitRequested => this._exitRequested.Token;

    public void RequestExit() => this._exitRequested.Cancel();

    /// <summary>Once the user has seen it there is nothing left to do, so this also asks the app to exit.</summary>
    public void InformUserAppIsAlreadyRunning()
    {
        MessageBox.Show("FZScout is already running. Look for its icon in the system tray.", "FZScout", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestExit();
    }

    public void Dispose() => this._exitRequested.Dispose();
}
