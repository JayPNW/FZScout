namespace FZScout.Logs;

/// <summary>The player killed something. Kills by anyone else don't trigger a bonus message. See <see cref="PetKillLogEvent"/> for pets.</summary>
record KillLogEvent : LogEvent
{
    public static LogEvent? TryParse(string message)
    {
        if (message.StartsWith("You have slain ", StringComparison.Ordinal))
        {
            return new KillLogEvent();
        }

        return null;
    }
}
