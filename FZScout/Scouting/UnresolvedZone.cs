namespace FZScout.Scouting;

/// <summary>A zone entered while the server's zones list wasn't loaded. It's resolved once the list loads.</summary>
record UnresolvedZone(string LogName) : Zone;
