namespace FZScout.Tests;

/// <summary>A user who asks to exit when <paramref name="exitRequested"/> is cancelled, and sees that the app is already running whenever told.</summary>
class InMemoryUserInput(CancellationToken exitRequested) : IUserInput
{
    public CancellationToken ExitRequested => exitRequested;

    public int TimesInformedAppIsAlreadyRunning { get; private set; }

    public void InformUserAppIsAlreadyRunning() => TimesInformedAppIsAlreadyRunning++;
}
