namespace FZScout;

/// <summary>Keeps two copies of FZScout from running at once, which would report everything twice.</summary>
static class SingleInstance
{
    public static IDisposable? TryClaim()
    {
        var mutex = new Mutex(true, "FZScout", out var isOnlyInstance);
        if (isOnlyInstance)
        {
            return mutex;
        }

        mutex.Dispose();
        return null;
    }
}
