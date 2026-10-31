using FZScout.Site;

namespace FZScout.Tests.Site;

/// <summary>Uses the real Windows Credential Manager, under a throwaway name so the user's key is untouched.</summary>
[CategoryTrait("Integration")]
public class WindowsCredentialManagerApiKeyStoreTests : ApiKeyStoreContract, IDisposable
{
    public WindowsCredentialManagerApiKeyStoreTests()
        : base(new WindowsCredentialManagerApiKeyStore($"FZScout/ApiKey/Test/{Guid.NewGuid()}"))
    {
    }

    public void Dispose() => this.Store.Delete();
}
