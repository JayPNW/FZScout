using Meziantou.Framework.Win32;

namespace FZScout.Site;

class WindowsCredentialManagerApiKeyStore(string credentialName = "FZScout/ApiKey") : IApiKeyStore
{
    public string StorageLocationForDisplay => "Windows Credential Manager";

    public ApiKey? Load()
    {
        var key = CredentialManager.ReadCredential(credentialName)?.Password;
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        return new ApiKey(key);
    }

    public void Save(ApiKey apiKey)
        => CredentialManager.WriteCredential(credentialName, Environment.UserName, apiKey.Value, CredentialPersistence.LocalMachine);

    public void Delete() => CredentialManager.TryDeleteCredential(credentialName);
}
