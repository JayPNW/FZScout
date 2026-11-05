namespace FZScout;

/// <summary>What the app is doing, for the UI to show however it likes.</summary>
enum ScoutingStatus
{
    NotScouting,
    Stopped,
    Scouting,
    ScoutingAfterProblem,
    WaitingForBonusMessage,
    WaitingForBonusMessageAfterProblem,
}
