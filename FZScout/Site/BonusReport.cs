namespace FZScout.Site;

/// <param name="Zone">The site's zone name.</param>
record BonusReport(Server Server, string Zone, BonusKey Bonus)
{
    public override string ToString() => $"{Server} '{Zone}' is '{Bonus}'";
}
