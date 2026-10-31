using FZScout.Site;

namespace FZScout.Scouting;

record ReportBonusAction(Character Character, BonusReport Report) : ReportAction(Character, Report);
