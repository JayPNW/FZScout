using System.Runtime.CompilerServices;
using FZScout.Scouting;

namespace FZScout.Tests.Scouting;

public class ZoneNamesTests
{
    /// <summary>Generates the zone name mapping, linked from InternalDocumentation/Zones.md, from the sites' zone lists.</summary>
    [Fact]
    public Task ZoneNamesMapping()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Site", "SiteZonesIntegrationTests.KnownZones.verified.txt");
        var siteZones = File.ReadAllLines(path).Where(line => line.Length > 0).ToHashSet();
        var logNames = ZoneNames.LogToSite.ToLookup(pair => pair.Value, pair => pair.Key);

        // Zones with a different log name, then zones whose names match.
        static IEnumerable<(int Group, string LogName, string Site)> ToRows(
            string zone, ILookup<string, string> logNames, HashSet<string> siteZones)
        {
            if (logNames.Contains(zone))
            {
                return logNames[zone].Select(logName =>
                    (0, logName, Describe(ZoneNames.Resolve(logName, siteZones))));
            }

            if (ZoneNames.LogToSite.ContainsKey(zone))
            {
                // Listed above under its log name, which maps it to another site zone.
                return [];
            }

            return [(1, "<same>", Describe(ZoneNames.Resolve(zone, siteZones)))];
        }

        var rows = from zone in siteZones
                   from r in ToRows(zone, logNames, siteZones)
                   orderby r.Group, r.Group == 0 ? r.LogName : r.Site
                   select (r.LogName, r.Site);

        return Verify(FormatAsMarkdownTable(("EQ log message name", "Site zone name"), rows));
    }

    static string FormatAsMarkdownTable<TTuple>(TTuple header, IEnumerable<TTuple> rows)
        where TTuple : ITuple
    {
        static string[] ToCells(ITuple row)
            => [.. Enumerable.Range(0, row.Length).Select(i => row[i]?.ToString() ?? "")];

        static string ToLine(IEnumerable<string> cells, string padding)
            => $"|{padding}{string.Join($"{padding}|{padding}", cells)}{padding}|";

        var all = rows.Prepend(header).Select(row => ToCells(row)).ToList();
        var widths = Enumerable.Range(0, header.Length)
            .Select(column => all.Max(cells => cells[column].Length))
            .ToList();
        var lines = all
            .Select(cells => ToLine(cells.Select((cell, column) => cell.PadRight(widths[column])), " "))
            .ToList();
        lines.Insert(1, ToLine(widths.Select(width => new string('-', width)), "-"));

        return string.Join(Environment.NewLine, lines);
    }

    static string Describe(Zone zone)
    {
        return zone switch
        {
            KnownZone known => known.SiteName,
            UnknownZone => "<unknown>",
            UnresolvedZone => "<unresolved>",
            _ => throw new ArgumentOutOfRangeException(nameof(zone)),
        };
    }
}
