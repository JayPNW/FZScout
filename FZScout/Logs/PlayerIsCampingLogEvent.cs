using System.Text.RegularExpressions;

namespace FZScout.Logs;

/// <summary>The player is camping out. Repeats every 5 seconds during the countdown.</summary>
partial record PlayerIsCampingLogEvent : LogEvent
{
    [GeneratedRegex(@"^It will take .+ to prepare your camp\.$", RegexOptions.CultureInvariant)]
    private static partial Regex CampingMessagePattern();

    public static LogEvent? TryParse(string message)
    {
        if (CampingMessagePattern().IsMatch(message))
        {
            return new PlayerIsCampingLogEvent();
        }

        return null;
    }
}
