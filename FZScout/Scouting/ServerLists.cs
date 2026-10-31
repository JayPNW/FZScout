namespace FZScout.Scouting;

record ServerLists(IReadOnlySet<BonusKey> Types, IReadOnlySet<string> Zones);
