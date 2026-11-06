using FZScout.Reporting;
using FZScout.Scouting;
using FZScout.Site;
using FZScout.Tests.Reporting;
using Microsoft.Extensions.Time.Testing;

namespace FZScout.Tests.Scouting;

/// <summary>Bob on Frostreaver, with a scout that reports to a fake site, and helpers to feed it log messages.</summary>
public abstract class CharacterScoutTestFixture
{
    private protected const string Befallen = "You have entered Befallen.";
    private protected const string Crushbone = "You have entered Clan Crushbone.";
    private protected const string Kill = "You have slain a skeleton!";
    private protected const string Experience = "This land is saturated with unstable magic, improving the experiences of all who venture here.";
    private protected const string Tribute = "This land is saturated with unstable magic, enhancing the potency of any offerings made here.";
    private protected const string DailyReset = "Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources.";
    private protected const string Camping = "It will take you about 30 seconds to prepare your camp.";
    private protected const string Chat = "You say, 'hello'";
    private protected const string CampAbandoned = "You abandon your preparations to camp.";

    private protected static readonly Character Bob = new("Bob", Server.Frostreaver);
    private protected static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private protected static readonly ServerLists ListsWithoutTributeLikeFrostreaver = new(
        new HashSet<BonusKey> { new("experience"), new("aa"), new("loot"), BonusKey.None },
        new HashSet<string> { "Befallen", "Crushbone", "Greater Faydark" });

    private protected static readonly EverQuestLogWatcher.EverQuestLogLineParser Parser = new(new EverQuestLogWatcher.Character(Bob.Name, Bob.Server.Name));

    readonly FakeSite _site = new();
    readonly StringWriter _output = new();

    protected CharacterScoutTestFixture()
    {
        var log = new Log(this._output, new FakeTimeProvider(Start));
        var bonusReporter = new BonusReporter(this._site.Client, () => new ApiKey("fz_live_test"), log);
        Scout = new(Bob, log, bonusReporter);
    }

    private protected CharacterScout Scout { get; }

    private protected static string Report(string zone, string bonus) => $"Reported Frostreaver '{zone}' is '{bonus}'.";

    private protected static string ReportNone(string zone) => $"Reported Frostreaver '{zone}' is 'none'.";

    private protected Task<IReadOnlyList<string>> Log(int seconds, string message) => Handle(seconds, message, ListsWithoutTributeLikeFrostreaver);

    private protected Task<IReadOnlyList<string>> LogWithoutLists(int seconds, string message) => Handle(seconds, message, null);

    /// <returns>The lines the scout logged for this message, without their timestamps.</returns>
    async Task<IReadOnlyList<string>> Handle(int seconds, string message, ServerLists? lists)
    {
        this._output.GetStringBuilder().Clear();
        await Scout.HandleAsync(Parser.Parse($"[Thu Oct 29 12:00:00 2026] {message}"), Start.AddSeconds(seconds), lists, TestContext.Current.CancellationToken);
        return [.. this._output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => line["12:00:00 ".Length..])];
    }
}
