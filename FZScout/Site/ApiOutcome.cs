namespace FZScout.Site;

public enum ApiOutcome
{
    Success,

    /// <summary>401: missing or invalid API key.</summary>
    KeyRejected,

    /// <summary>Any other status, a network failure, or a response we couldn't read.</summary>
    Failed,
}
