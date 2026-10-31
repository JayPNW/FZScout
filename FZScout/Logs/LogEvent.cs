namespace FZScout.Logs;

/// <summary>A log line that matters to scouting. Each kind knows how to recognize its own message.</summary>
abstract record LogEvent
{
    static readonly Func<string, LogEvent?>[] Parsers =
    [
        ZoneEnteredLogEvent.TryParse,
        KillLogEvent.TryParse,
        BonusReportLogEvent.TryParse,
        DailyResetLogEvent.TryParse,
        PlayerIsCampingLogEvent.TryParse,
        CampAbandonedLogEvent.TryParse,
    ];

    /// <summary>Parses a raw log line, timestamp included. Returns null for lines that don't matter.</summary>
    public static LogEvent? Parse(string line)
    {
        var message = LogLine.StripTimestamp(line);
        return Parsers.Select(parse => parse(message)).FirstOrDefault(e => e is not null);
    }
}
