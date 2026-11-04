using FZScout.Site;

namespace FZScout;

/// <summary>Gets an API key the site accepts: the stored one, or else asks the user until the site accepts one or the user enters nothing.</summary>
class ApiKeyPrompt(FangbreakerZoneClient fangbreakerZoneClient, IApiKeyStore apiKeyStore, Log log, Func<CancellationToken, Task<ApiKey?>> readApiKeyAsync)
{
    /// <summary>Checks a key by posting 'none' for a zone that never has a bonus.</summary>
    static readonly BonusReport KeyCheck = new(Server.Fangbreaker, "The Nexus", BonusKey.None);

    /// <returns>Null if the user entered nothing.</returns>
    public async Task<ApiKey?> GetAcceptedApiKeyAsync(CancellationToken cancellationToken)
    {
        if (apiKeyStore.Load() is { } storedApiKey && await IsAcceptedAsync(storedApiKey, cancellationToken))
        {
            return storedApiKey;
        }

        while (true)
        {
            log.Info("Get an API key from https://fangbreaker.zone/account and paste it into Settings. (For help see https://jaypnw.github.io/FZScout/create-api-key.html):");
            var answer = await readApiKeyAsync(cancellationToken);
            if (answer is not { } apiKey)
            {
                log.Error("No API key entered. Exiting.");
                return null;
            }

            Save(apiKey);
            if (await IsAcceptedAsync(apiKey, cancellationToken))
            {
                return apiKey;
            }
        }
    }

    async Task<bool> IsAcceptedAsync(ApiKey apiKey, CancellationToken cancellationToken)
    {
        log.Info($"Checking the API key with {KeyCheck.Server.Site.Host}.");
        var result = await fangbreakerZoneClient.ReportBonusAsync(KeyCheck, apiKey, cancellationToken);
        if (result.IsSuccess)
        {
            log.Info("API key verified.");
            return true;
        }

        if (result.Outcome == ApiOutcome.KeyRejected)
        {
            log.Error(result.Error!);
            log.Error("API key rejected.");
            return false;
        }

        log.Warning($"API key: couldn't check it, so it will be used as is. {result.Error}");
        return true;
    }

    void Save(ApiKey apiKey)
    {
        try
        {
            apiKeyStore.Save(apiKey);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.Error($"Couldn't save the API key, so it will only be used until the app closes: {exception.Message}");
            return;
        }

        log.Info($"API key saved to {apiKeyStore.StorageLocationForDisplay}.");
    }
}
