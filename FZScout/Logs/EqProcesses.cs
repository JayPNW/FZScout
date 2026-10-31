using System.Diagnostics;

namespace FZScout.Logs;

static class EqProcesses
{
    public static IReadOnlyList<EqProcess> Find()
    {
        var processes = Process.GetProcessesByName("eqgame");
        try
        {
            return [..
                processes
                    .Select(EqProcess.FromProcess)
                    .OfType<EqProcess>()
                ];
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
