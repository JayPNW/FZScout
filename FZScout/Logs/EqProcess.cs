using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FZScout.Logs;

record EqProcess(int Id, string ExePath)
{
    const uint ProcessQueryLimitedInformation = 0x1000;

    public string LogsFolder => Path.Combine(Path.GetDirectoryName(ExePath)!, "Logs");

    public static EqProcess? FromProcess(Process process)
    {
        var path = ExePathOf(process.Id);
        if (path is null)
        {
            return null;
        }

        return new EqProcess(process.Id, path);
    }

    static string? ExePathOf(int processId)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var buffer = new StringBuilder(1024);
            var size = buffer.Capacity;
            // works even when EverQuest runs elevated, unlike Process.MainModule
            if (!QueryFullProcessImageName(handle, 0, buffer, ref size))
            {
                return null;
            }

            return buffer.ToString(0, size);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "QueryFullProcessImageNameW")]
    static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder exeName, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr handle);
}
