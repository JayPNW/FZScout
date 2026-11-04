using FZScout.Scouting;

namespace FZScout.Tests.Scouting;

public class CharacterScoutTests : CharacterScoutTestFixture
{
    [Fact]
    public async Task BonusMessageIsReported()
    {
        await Log(0, Befallen);

        Assert.Equal([Report("Befallen", "experience")], await Log(10, Experience));
    }

    [Fact]
    public async Task ZoneIsReportedByItsSiteName()
    {
        await Log(0, Crushbone);

        Assert.Equal([Report("Crushbone", "experience")], await Log(10, Experience));
    }

    [Fact]
    public async Task EnteringAZoneIsLogged()
    {
        Assert.Equal([$"Bob (Frostreaver) entered The Greater Faydark, waiting until {Start.AddMinutes(5).ToLocalTime():HH:mm:ss} for a bonus message."], await Log(0, "You have entered The Greater Faydark."));
    }

    [Fact]
    public async Task BonusBeforeAnyZoneIsIgnored()
    {
        Assert.Empty(await Log(0, Experience));
        Assert.Empty(await Log(1000, Chat));
    }

    [Fact]
    public async Task BonusTypeNotOnTheServersListIsAnError()
    {
        await Log(0, Befallen);

        Assert.Equal(
            [$"ERROR: Bob (Frostreaver) saw a 'tribute' bonus in 'Befallen', which isn't on the Frostreaver bonus types list. Please report it at {CharacterScout.ReportIssuesUrl}"],
            await Log(10, Tribute));
    }

    [Fact]
    public async Task BonusTypeNotOnTheServersListMeansNoneIsntReported()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(21, Tribute);

        Assert.Empty(await Log(25, Chat));
        Assert.Empty(await Log(300, Chat));
    }

    [Fact]
    public async Task UnknownZoneWithNoBonusIsNotAnError()
    {
        Assert.Equal(
            ["Bob (Frostreaver) entered The Plane of Knowledge."],
            await Log(0, "You have entered The Plane of Knowledge."));
        Assert.Empty(await Log(10, Kill));
        Assert.Empty(await Log(1000, Chat));
    }

    [Fact]
    public async Task BonusInAnUnknownZoneIsAnError()
    {
        await Log(0, "You have entered The Halls of Honor.");

        Assert.Equal(
            [$"ERROR: Bob (Frostreaver) saw a 'experience' bonus in 'The Halls of Honor', which isn't on the Frostreaver zones list. Please report it at {CharacterScout.ReportIssuesUrl}"],
            await Log(10, Experience));
    }

    [Fact]
    public async Task ZoneEnteredWithoutListsIsResolvedWhenTheyLoad()
    {
        Assert.Equal([$"Bob (Frostreaver) entered Befallen, waiting until {Start.AddMinutes(5).ToLocalTime():HH:mm:ss} for a bonus message."], await LogWithoutLists(0, Befallen));
        Assert.Empty(await LogWithoutLists(60, Chat));

        Assert.Empty(await Log(61, Chat));
        Assert.Equal(new KnownZone("Befallen"), Scout.CurrentZone);
        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }

    [Fact]
    public async Task BonusAfterTheListsLoadIsReported()
    {
        await LogWithoutLists(0, Befallen);

        Assert.Equal([Report("Befallen", "experience")], await Log(10, Experience));
    }

    [Fact]
    public async Task ZoneEnteredWithoutListsThatTurnsOutUnknownStopsWaiting()
    {
        await LogWithoutLists(0, "You have entered The Halls of Honor.");

        Assert.Empty(await Log(60, Chat));
        Assert.Equal(new UnknownZone("The Halls of Honor"), Scout.CurrentZone);
        Assert.Empty(await Log(1000, Chat));
    }

    [Fact]
    public async Task ListsThatNeverLoadMeanNoneIsntReported()
    {
        await LogWithoutLists(0, Befallen);

        Assert.Equal(
            ["ERROR: Bob (Frostreaver) was in 'Befallen', but the Frostreaver zones list still isn't loaded, so 'none' wasn't reported."],
            await LogWithoutLists(300, Chat));
        Assert.Empty(await LogWithoutLists(1000, Chat));
    }

    [Fact]
    public async Task BonusWhileListsAreMissingIsAnError()
    {
        await LogWithoutLists(0, Befallen);

        Assert.Equal(
            ["ERROR: Bob (Frostreaver) saw a 'experience' bonus in 'Befallen', but the Frostreaver zones list isn't loaded, so it wasn't reported."],
            await LogWithoutLists(10, Experience));
    }

    [Fact]
    public async Task AreasInsideAZoneDontChangeIt()
    {
        await Log(0, Befallen);
        await Log(100, "You have entered an area where levitation effects do not function.");

        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }

    [Fact]
    public async Task ArenaAreasInsideAZoneDontChangeIt()
    {
        await Log(0, Befallen);
        await Log(100, "You have entered an Arena (PvP) area.");

        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }
}
