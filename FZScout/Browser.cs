using System.Diagnostics;

namespace FZScout;

static class Browser
{
    public static void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
