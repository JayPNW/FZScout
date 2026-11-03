namespace FZScout;

/// <summary>A site's bonus type key, like <c>experience</c> or <c>none</c>.</summary>
readonly record struct BonusKey(string Value)
{
    public static readonly BonusKey None = new("none");

    public static readonly BonusKey Unconfirmed = new("unconfirmed");

    static readonly Dictionary<string, BonusKey> KeysByAnnouncementText = new()
    {
        ["improving the experiences of all who venture here"] = new("experience"),
        ["improving the experience tied to abilities for all who venture here"] = new("aa"),
        ["improving the experience tied to items for all who venture here"] = new("item-exp"),
        ["multiplying the loot that may be found here"] = new("loot"),
        ["multiplying the coin that may be found here"] = new("coin"),
        ["which has drawn many creatures of exceptional rarity here"] = new("rare"),
        ["magnifying the recognition one may receive from their adventures here"] = new("faction"),
        ["which has drawn numerous mundane creatures here"] = new("respawn"),
        ["enhancing the benefits of offerings for all who venture here"] = new("tribute"),
        ["enhancing the potency of any offerings made here"] = new("tribute"),
        ["improving the rate at which proficiency is gained while adventuring here"] = new("tradeskill"),
    };

    public override string ToString() => Value;

    /// <summary>The bonus key for the text EverQuest's "This land is saturated with unstable magic, {text}." announcement names, or null if it's not recognized.</summary>
    public static BonusKey? FromAnnouncementText(string announcementText) => KeysByAnnouncementText.TryGetValue(announcementText, out var key) ? key : null;
}
