using FZScout.Site;

namespace FZScout.Scouting;

/// <summary>Report <c>none</c> for a zone, unless the site already shows a bonus for it.</summary>
record ReportNoBonusAction(Character Character, string Zone) : ReportAction(Character, new BonusReport(Character.Server, Zone, BonusKey.None));
