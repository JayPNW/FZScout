using System.Windows.Threading;

namespace FZScout.Tests.Xaml;

/// <summary>
/// One STA thread, running a dispatcher, shared by every test that creates WPF objects.
/// Sharing it lets objects cached in statics, like <see cref="WindowIcon"/>'s images, be used by more than one test.
/// </summary>
static class WpfThread
{
    static readonly Dispatcher Dispatcher = StartDispatcherThread();

    public static T Invoke<T>(Func<T> function) => Dispatcher.Invoke(function);

    /// <summary>Lets bindings and layout queued on the dispatcher finish. Call on the WPF thread.</summary>
    public static void RunQueuedWork() => Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    static Dispatcher StartDispatcherThread()
    {
        Dispatcher? dispatcher = null;
        using var started = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            started.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "WPF test thread",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        started.Wait();
        return dispatcher!;
    }
}
