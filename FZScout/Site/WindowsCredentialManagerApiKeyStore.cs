using System.Runtime.InteropServices;
using System.Text;

namespace FZScout.Site;

class WindowsCredentialManagerApiKeyStore : IApiKeyStore
{
    const string CredentialName = "FZScout/ApiKey";
    const int CredTypeGeneric = 1;
    const int CredPersistLocalMachine = 2;

    public string StorageLocationForDisplay => "Windows Credential Manager";

    public ApiKey? Load()
    {
        if (!CredRead(CredentialName, CredTypeGeneric, 0, out var credentialPointer))
        {
            return null;
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(credentialPointer);
            if (credential.CredentialBlobSize == 0)
            {
                return null;
            }

            var key = Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
            return new ApiKey(key);
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    public void Save(ApiKey apiKey)
    {
        var blob = Encoding.Unicode.GetBytes(apiKey.Value);
        var blobPointer = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobPointer, blob.Length);
            var credential = new Credential
            {
                Type = CredTypeGeneric,
                TargetName = CredentialName,
                CredentialBlobSize = blob.Length,
                CredentialBlob = blobPointer,
                Persist = CredPersistLocalMachine,
                UserName = Environment.UserName,
            };
            if (!CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException($"Couldn't save the API key to Windows Credential Manager (error {Marshal.GetLastWin32Error()}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blobPointer);
        }
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredReadW")]
    static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredWriteW")]
    static extern bool CredWrite(ref Credential credential, int flags);

    [DllImport("advapi32.dll")]
    static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Credential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }
}
