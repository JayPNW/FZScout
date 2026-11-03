using FZScout.Site;

namespace FZScout.Scouting;

/// <summary>Report <c>none</c> for a zone, unless the site already shows a bonus for it.</summary>
record ReportNoBonusAction(Server Server, string Zone) : ReportAction(new BonusReport(Server, Zone, BonusKey.None));
