namespace FZScout.Site;

/// <summary>The user's site API key. <see cref="ToString"/> hides it so it can't leak into the log.</summary>
readonly record struct ApiKey(string Value)
{
    const int VisibleStartLength = 12;
    const int VisibleEndLength = 6;
    const int ShortestKeyThatCanBeAbbreviatedWithoutRevealingMostOfIt = 32;

    /// <summary>Like fangbreaker.zone/account shows it: <c>fz_live_qa81...cUMfgC</c>. Enough to recognize the key, not enough to use it.</summary>
    public string AbbreviatedForDisplay
    {
        get
        {
            if (Value.Length < ShortestKeyThatCanBeAbbreviatedWithoutRevealingMostOfIt)
            {
                return ToString();
            }

            return $"{Value[..VisibleStartLength]}...{Value[^VisibleEndLength..]}";
        }
    }

    public override string ToString() => "fz_live_****";
}
