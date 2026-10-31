using System.Text.RegularExpressions;

namespace FZScout.Logs;

static partial class LogLine
{
    [GeneratedRegex(@"^\[[^\]]*\] ", RegexOptions.CultureInvariant)]
    private static partial Regex TimestampPrefixPattern();

    /// <summary>Removes the leading <c>[Mon Sep 14 16:10:29 2026] </c>.</summary>
    public static string StripTimestamp(string line)
    {
        return TimestampPrefixPattern().Replace(line, "", 1);
    }
}
