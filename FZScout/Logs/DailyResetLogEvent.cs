namespace FZScout.Logs;

/// <summary>The server reset every zone's bonus for the new day.</summary>
record DailyResetLogEvent : LogEvent
{
    public static LogEvent? TryParse(string message)
    {
        if (message == "Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources.")
        {
            return new DailyResetLogEvent();
        }

        return null;
    }
}
