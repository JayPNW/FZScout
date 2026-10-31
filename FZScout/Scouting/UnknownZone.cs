namespace FZScout.Scouting;

/// <summary>A zone we can't match to the site's zones list.</summary>
record UnknownZone(string LogName) : Zone;
