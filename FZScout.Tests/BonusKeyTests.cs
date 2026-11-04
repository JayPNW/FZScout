namespace FZScout.Tests;

public class BonusKeyTests
{
    [Theory]
    [InlineData("improving the experiences of all who venture here", "experience")]
    [InlineData("improving the experience tied to abilities for all who venture here", "aa")]
    [InlineData("improving the experience tied to items for all who venture here", "item-exp")]
    [InlineData("multiplying the loot that may be found here", "loot")]
    [InlineData("multiplying the coin that may be found here", "coin")]
    [InlineData("which has drawn many creatures of exceptional rarity here", "rare")]
    [InlineData("magnifying the recognition one may receive from their adventures here", "faction")]
    [InlineData("which has drawn numerous mundane creatures here", "respawn")]
    [InlineData("enhancing the benefits of offerings for all who venture here", "tribute")]
    [InlineData("enhancing the potency of any offerings made here", "tribute")]
    [InlineData("improving the rate at which proficiency is gained while adventuring here", "tradeskill")]
    public void RecognizesTheRealEverQuestAnnouncementTextForEachKnownBonusKey(string announcementText, string key)
    {
        Assert.Equal(new BonusKey(key), BonusKey.FromAnnouncementText(announcementText));
    }

    [Fact]
    public void UnrecognizedAnnouncementIsNull()
    {
        Assert.Null(BonusKey.FromAnnouncementText("something EverQuest has never said before"));
    }
}
