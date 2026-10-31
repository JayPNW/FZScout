namespace FZScout.Site;

public enum ApiOutcome
{
    Success,

    /// <summary>400.</summary>
    BadPayload,

    /// <summary>401: missing or invalid API key.</summary>
    KeyRejected,

    /// <summary>403: the account is banned or timed out.</summary>
    Banned,

    /// <summary>404: the zone or server was not found.</summary>
    NotFound,

    /// <summary>500, any other status, a network failure, or a response we couldn't read.</summary>
    Unavailable,
}
