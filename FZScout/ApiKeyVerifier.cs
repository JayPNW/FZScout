using FZScout.Site;

namespace FZScout;

/// <summary>Saves an API key only once the site accepts it. Logs the details of anything that goes wrong.</summary>
class ApiKeyVerifier(IApiKeyStore apiKeyStore, FangbreakerZoneClient fangbreakerZoneClient, Log log)
{
    /// <summary>Raised only after the site accepted the key.</summary>
    public event Action? ApiKeySaved;

    public ApiKey? SavedApiKey { get; private set; } = apiKeyStore.Load();

    public async Task<ApiKeyVerificationResult> VerifyThenSaveAsync(ApiKey apiKey)
    {
        var result = await fangbreakerZoneClient.CheckApiKeyAsync(apiKey);
        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            return ApiKeyVerificationResult.RejectedBySite;
        }

        if (!result.IsSuccess)
        {
            log.Warning($"Couldn't check the API key with the site: {result.Error}");
            return ApiKeyVerificationResult.CouldNotCheckWithSite;
        }

        try
        {
            apiKeyStore.Save(apiKey);
        }
        catch (Exception exception)
        {
            log.Warning($"Couldn't save the API key to {apiKeyStore.StorageLocationForDisplay}: {exception.Message}");
            return ApiKeyVerificationResult.CouldNotSave;
        }

        SavedApiKey = apiKey;
        log.Info($"API key verified and saved to {apiKeyStore.StorageLocationForDisplay}.");
        ApiKeySaved?.Invoke();
        return ApiKeyVerificationResult.VerifiedAndSaved;
    }
}
