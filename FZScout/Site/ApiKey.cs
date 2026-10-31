namespace FZScout.Site;

/// <summary>The user's site API key. <see cref="ToString"/> hides it so it can't leak into the log.</summary>
readonly record struct ApiKey(string Value)
{
    public override string ToString() => "fz_live_****";
}
