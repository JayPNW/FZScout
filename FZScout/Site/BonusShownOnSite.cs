namespace FZScout.Site;

/// <summary>What the site shows today for a zone.</summary>
/// <param name="Zone">The site's zone name.</param>
record BonusShownOnSite(Server Server, string Zone, BonusKey Bonus);
