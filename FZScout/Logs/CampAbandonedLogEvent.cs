namespace FZScout.Logs;

record CampAbandonedLogEvent : LogEvent
{
    public static LogEvent? TryParse(string message)
    {
        if (message == "You abandon your preparations to camp.")
        {
            return new CampAbandonedLogEvent();
        }

        return null;
    }
}
