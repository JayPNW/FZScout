namespace FZScout.Scouting;

/// <summary>Forget any of this character's reports still waiting to retry; they're stale after the daily reset.</summary>
record DropPendingReportsAction(Character Character) : ScoutAction;
