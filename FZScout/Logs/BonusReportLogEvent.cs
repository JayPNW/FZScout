namespace FZScout.Logs;

/// <summary>EverQuest announced the current zone's bonus.</summary>
record BonusReportLogEvent(BonusKey BonusKey) : LogEvent
{
    const string Prefix = "This land is saturated with unstable magic, ";

    static readonly Dictionary<string, BonusKey> Keys = new()
    {
        [Prefix + "improving the experiences of all who venture here."] = new("experience"),
        [Prefix + "improving the experience tied to abilities for all who venture here."] = new("aa"),
        [Prefix + "multiplying the loot that may be found here."] = new("loot"),
        [Prefix + "multiplying the coin that may be found here."] = new("coin"),
        [Prefix + "which has drawn many creatures of exceptional rarity here."] = new("rare"),
        [Prefix + "magnifying the recognition one may receive from their adventures here."] = new("faction"),
        [Prefix + "which has drawn numerous mundane creatures here."] = new("respawn"),
        [Prefix + "enhancing the benefits of offerings for all who venture here."] = new("tribute"),
        [Prefix + "enhancing the potency of any offerings made here."] = new("tribute"),
        [Prefix + "improving the rate at which proficiency is gained while adventuring here."] = new("tradeskill"),
    };

    public static LogEvent? TryParse(string message)
    {
        if (Keys.TryGetValue(message, out var key))
        {
            return new BonusReportLogEvent(key);
        }

        return null;
    }
}
