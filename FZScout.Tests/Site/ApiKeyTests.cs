using FZScout.Site;

namespace FZScout.Tests.Site;

public class ApiKeyTests
{
    [Fact]
    public void AbbreviatesLikeTheSiteDoes()
    {
        var apiKey = new ApiKey("fz_live_qa81aaaaaaaaaaaaaaaaaaaaaaaaaaaacUMfgC");

        Assert.Equal("fz_live_qa81...cUMfgC", apiKey.AbbreviatedForDisplay);
    }

    [Fact]
    public void HidesAShortKeyEntirelyRatherThanRevealMostOfIt()
    {
        var apiKey = new ApiKey("fz_live_qa81cUMfgC");

        Assert.Equal("fz_live_****", apiKey.AbbreviatedForDisplay);
    }
}
