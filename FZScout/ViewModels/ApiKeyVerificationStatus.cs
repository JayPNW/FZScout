namespace FZScout.ViewModels;

/// <summary>What the Settings window shows about the API key being edited. When checking with the site or saving fails, the log has the details.</summary>
enum ApiKeyVerificationStatus
{
    NotVerified,
    Verifying,
    VerifiedAndSaved,
    RejectedBySite,
    CouldNotCheckWithSite,
    CouldNotSave,
}
