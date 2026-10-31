namespace FZScout.Site;

interface IApiKeyStore
{
    /// <summary>Where the key is kept, for telling the user, like "Windows Credential Manager".</summary>
    string StorageLocationForDisplay { get; }
    ApiKey? Load();
    void Save(ApiKey apiKey);
}
