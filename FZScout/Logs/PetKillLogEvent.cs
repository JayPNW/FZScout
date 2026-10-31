using System.Text.RegularExpressions;

namespace FZScout.Logs;

/// <summary>A pet or warder killed something. It counts as its owner's kill, so it triggers a bonus message for the owner only.</summary>
partial record PetKillLogEvent(string OwnerName) : LogEvent
{
    public static LogEvent? TryParse(string message)
    {
        var match = PetKillMessage().Match(message);
        return match.Success ? new PetKillLogEvent(match.Groups["owner"].Value) : null;
    }

    [GeneratedRegex(@" was slain by (?<owner>[^ ]+)`s (?:pet|warder)!$")]
    private static partial Regex PetKillMessage();
}
