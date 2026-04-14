using Microsoft.EntityFrameworkCore;
using sins.Data;

namespace sins.Services.Dnssec;

internal static class DnssecZoneDataLoader
{
    public static async Task<(List<string> SortedOwners, Dictionary<string, HashSet<ushort>> TypesByOwner)> LoadAsync(
        DnsContext context,
        string apex,
        CancellationToken cancellationToken = default)
    {
        var apexN = DnsCanonical.NormalizeOwner(apex);
        var all = await context.DnsRecords.AsNoTracking().ToListAsync(cancellationToken);
        var inZone = all.Where(r => DnsCanonical.NameUnderApex(r.Name, apexN)).ToList();
        var sorted = DnssecNsecChain.BuildSortedDistinctOwners(inZone.Select(r => r.Name), apexN);
        var types = new Dictionary<string, HashSet<ushort>>(StringComparer.Ordinal);
        foreach (var r in inZone)
        {
            var name = DnsCanonical.NormalizeOwner(r.Name);
            if (!types.TryGetValue(name, out var set))
            {
                set = new HashSet<ushort>();
                types[name] = set;
            }

            var code = TypeCode(r.Type);
            if (code != 0)
                set.Add(code);
        }

        return (sorted, types);
    }

    private static ushort TypeCode(string type) =>
        type.ToUpperInvariant() switch
        {
            "A" => DnsTypes.A,
            "NS" => DnsTypes.Ns,
            "CNAME" => DnsTypes.Cname,
            "SOA" => DnsTypes.Soa,
            "MX" => DnsTypes.Mx,
            "TXT" => DnsTypes.Txt,
            "AAAA" => 28,
            _ => 0
        };
}
