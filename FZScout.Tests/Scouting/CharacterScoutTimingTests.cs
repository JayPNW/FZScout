namespace FZScout.Tests.Scouting;

/// <summary>When <c>none</c> is reported. See the Timing section of InternalDocumentation/SPEC.md.</summary>
public class CharacterScoutTimingTests : CharacterScoutTestFixture
{
    [Fact]
    public async Task NoBonusFiveMinutesAfterEnteringIsReportedAsNone()
    {
        await Log(0, Befallen);

        Assert.Empty(await Log(299, Chat));
        Assert.Equal([ReportNone("Befallen")], await Log(300, Chat));
    }

    [Fact]
    public async Task NoneIsReportedOnlyOnce()
    {
        await Log(0, Befallen);
        await Log(300, Chat);

        Assert.Empty(await Log(600, Chat));
    }

    [Fact]
    public async Task NoBonusFiveSecondsAfterAKillIsReportedAsNone()
    {
        await Log(0, Befallen);
        await Log(20, Kill);

        Assert.Empty(await Log(24, Chat));
        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
        Assert.Empty(await Log(300, Chat));
    }

    [Fact]
    public async Task KillByOwnPetCountsAsAKill()
    {
        await Log(0, Befallen);
        await Log(20, "A wolf was slain by Bob`s pet!");

        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
    }

    [Fact]
    public async Task KillByOwnWarderCountsAsAKill()
    {
        await Log(0, Befallen);
        await Log(20, "A gnoll pup was slain by Bob`s warder!");

        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
    }

    [Fact]
    public async Task KillByAnotherPlayersPetIsNotAKill()
    {
        await Log(0, Befallen);
        await Log(20, "A wolf was slain by Alice`s pet!");

        Assert.Empty(await Log(25, Chat));
    }

    [Fact]
    public async Task LaterKillsDoNotPushTheKillTimerBack()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(23, Kill);

        Assert.Equal([ReportNone("Befallen")], await Log(25, Chat));
    }

    [Fact]
    public async Task KillsAfterABonusDoNothing()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(20, Experience);
        await Log(40, Kill);

        Assert.Empty(await Log(300, Chat));
    }

    [Fact]
    public async Task BonusMessageArrivingMinutesAfterAKillIsReportedEvenAfterReportingNone()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(25, Chat);

        Assert.Equal([Report("Befallen", "experience")], await Log(200, Experience));
    }

    [Fact]
    public async Task EnteringAnotherZoneRestartsTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(100, Crushbone);

        Assert.Empty(await Log(300, Chat));
        Assert.Equal([ReportNone("Crushbone")], await Log(400, Chat));
    }

    [Fact]
    public async Task EnteringAnotherZoneCancelsTheKillTimer()
    {
        await Log(0, Befallen);
        await Log(20, Kill);
        await Log(22, Crushbone);

        Assert.Empty(await Log(30, Chat));
    }

    [Fact]
    public async Task ReEnteringTheSameZoneStartsANewVisit()
    {
        await Log(0, Befallen);
        await Log(10, Experience);
        await Log(100, Befallen);

        Assert.Equal([ReportNone("Befallen")], await Log(400, Chat));
    }

    [Fact]
    public async Task DailyResetRestartsTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(10, Experience);
        await Log(1000, DailyReset);

        Assert.Empty(await Log(1299, Chat));
        Assert.Equal([ReportNone("Befallen")], await Log(1300, Chat));
    }

    [Fact]
    public async Task DailyResetRearmsKills()
    {
        await Log(0, Befallen);
        await Log(10, Experience);
        await Log(1000, DailyReset);
        await Log(1010, Kill);

        Assert.Equal([ReportNone("Befallen")], await Log(1015, Chat));
    }

    [Fact]
    public async Task CampingStopsTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(200, Camping);
        await Log(205, "It will take about 25 more seconds to prepare your camp.");

        Assert.Empty(await Log(10_000, Chat));
    }

    [Fact]
    public async Task AbandoningCampDoesNotRestartTheIdleTimer()
    {
        await Log(0, Befallen);
        await Log(200, Camping);
        await Log(210, CampAbandoned);

        Assert.Empty(await Log(10_000, Chat));
    }
}
