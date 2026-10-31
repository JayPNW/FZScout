namespace FZScout.Site;

/// <summary>Keeps the API key only as long as this object lives.</summary>
class InMemoryApiKeyStore : IApiKeyStore
{
    ApiKey? _apiKey;

    public string StorageLocationForDisplay => "memory";

    public ApiKey? Load() => this._apiKey;

    public void Save(ApiKey apiKey) => this._apiKey = apiKey;

    public void Delete() => this._apiKey = null;
}
