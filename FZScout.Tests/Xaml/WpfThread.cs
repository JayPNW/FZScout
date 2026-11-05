using System.Windows.Threading;

namespace FZScout.Tests.Xaml;

/// <summary>
/// One STA thread, running a dispatcher, shared by every test that creates WPF objects.
/// Sharing it lets objects cached in statics, like <see cref="WindowIcon"/>'s images, be used by more than one test.
/// </summary>
static class WpfThread
{
    static readonly Dispatcher Dispatcher = StartDispatcherThread();

    static readonly SemaphoreSlim OneTestAtATime = new(1, 1);

    /// <summary>
    /// Runs one test's function at a time, waiting on the test's own thread.
    /// Without that, waiting in <see cref="RunQueuedWork"/> would run other tests' functions in the middle of this one.
    /// Don't call on the WPF thread.
    /// </summary>
    public static T Invoke<T>(Func<T> function)
    {
        OneTestAtATime.Wait();
        try
        {
            return Dispatcher.Invoke(function);
        }
        finally
        {
            OneTestAtATime.Release();
        }
    }

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
