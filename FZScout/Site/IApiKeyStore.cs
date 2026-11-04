namespace FZScout.Site;

interface IApiKeyStore
{
    string StorageLocationForDisplay { get; }
    ApiKey? Load();
    void Save(ApiKey apiKey);
    void Delete();
}
