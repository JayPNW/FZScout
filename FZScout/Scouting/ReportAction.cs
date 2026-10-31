using FZScout.Site;

namespace FZScout.Scouting;

abstract record ReportAction(Character Character, BonusReport Report) : ScoutAction;
