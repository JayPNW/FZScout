namespace FZScout;

/// <summary>Keeps two copies of FZScout from running at once, which would report everything twice.</summary>
static class SingleInstance
{
    /// <returns>A claim held until disposed, or null if another FZScout already holds it.</returns>
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
