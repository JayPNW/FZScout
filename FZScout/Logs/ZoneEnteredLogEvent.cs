using System.Text.RegularExpressions;

namespace FZScout.Logs;

partial record ZoneEnteredLogEvent(string Zone) : LogEvent
{
    static readonly string[] KnownNotAZoneMessages =
    [
        "You have entered an area where levitation effects do not function.",
        "You have entered an area where Bind Affinity is allowed.",
        "You have entered an Arena (PvP) area.",
    ];

    [GeneratedRegex(@"^You have entered (?<zone>.*)\.$", RegexOptions.CultureInvariant)]
    private static partial Regex ZoneEnteredPattern();

    public static LogEvent? TryParse(string message)
    {
        if (KnownNotAZoneMessages.Contains(message))
        {
            return null;
        }

        var match = ZoneEnteredPattern().Match(message);
        if (!match.Success)
        {
            return null;
        }

        var zone = match.Groups["zone"].Value;

        return new ZoneEnteredLogEvent(zone);
    }
}
