namespace FZScout;

/// <summary>When checking with the site or saving fails, the log has the details.</summary>
enum ApiKeyVerificationResult
{
    VerifiedAndSaved,
    RejectedBySite,
    CouldNotCheckWithSite,
    CouldNotSave,
}
