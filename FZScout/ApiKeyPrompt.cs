using FZScout.Reporting;
using FZScout.Site;

namespace FZScout;

/// <summary>Asks the user for an API key until the site accepts one or the user enters nothing.</summary>
class ApiKeyPrompt(BonusReporter bonusReporter, IApiKeyStore apiKeyStore, ConsoleReporter log, Func<CancellationToken, Task<ApiKey?>> readApiKeyAsync)
{
    public async Task AskUntilAcceptedAsync(CancellationToken cancellationToken)
    {
        while (!bonusReporter.HasApiKey)
        {
            log.Info("Get an API key from https://fangbreaker.zone/account and paste it here. (For help see https://jaypnw.github.io/FZScout/create-api-key.html):");
            var answer = await readApiKeyAsync(cancellationToken);
            if (answer is not { } apiKey)
            {
                log.Error("No API key entered. Exiting.");
                return;
            }

            Save(apiKey);
            await bonusReporter.ChangeApiKeyAsync(apiKey, cancellationToken);
        }
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
