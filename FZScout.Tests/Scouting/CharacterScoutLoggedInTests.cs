namespace FZScout.Tests.Scouting;

public class CharacterScoutLoggedInTests : CharacterScoutTestFixture
{
    [Fact]
    public void IsNotLoggedInUntilTheCharacterLogsSomething()
    {
        Assert.Null(Scout.LoggedInUntil);
    }

    [Fact]
    public async Task IsLoggedInOnceTheCharacterLogsSomething()
    {
        await Log(0, Chat);

        Assert.Equal(DateTimeOffset.MaxValue, Scout.LoggedInUntil);
    }

    [Fact]
    public async Task IsLoggedInUntilTheCampFinishes()
    {
        await Log(0, Camping);
        await Log(5, "It will take about 25 more seconds to prepare your camp.");

        Assert.Equal(Start.AddSeconds(30), Scout.LoggedInUntil);
    }

    [Fact]
    public async Task IsLoggedInAgainAfterAbandoningCamp()
    {
        await Log(0, Camping);
        await Log(5, CampAbandoned);

        Assert.Equal(DateTimeOffset.MaxValue, Scout.LoggedInUntil);
    }

    [Fact]
    public async Task IsNotLoggedInOnceEverQuestExits()
    {
        await Log(0, Chat);

        Scout.ForgetBeingLoggedIn();

        Assert.Null(Scout.LoggedInUntil);
    }
}
