namespace sins.Services.Dnssec;

internal static class DnssecNsecChain
{
    public static List<string> BuildSortedDistinctOwners(IEnumerable<string> recordNames, string apex)
    {
        var s = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in recordNames)
        {
            var nn = DnsCanonical.NormalizeOwner(n);
            if (nn.Length == 0) continue;
            if (DnsCanonical.NameUnderApex(nn, apex))
                s.Add(nn);
        }

        s.Add(DnsCanonical.NormalizeOwner(apex));
        var list = s.ToList();
        DnsCanonical.SortNames(list);
        return list;
    }

    /// <summary>Find NSEC owner and next for NXDOMAIN: <paramref name="q"/> is not an owner in the zone.</summary>
    public static (string owner, string next) FindCoveringNsec(string q, IReadOnlyList<string> sorted)
    {
        if (sorted.Count == 0)
            throw new InvalidOperationException("NSEC chain requires at least the apex.");

        var Q = DnsCanonical.NormalizeOwner(q);
        if (sorted.Count == 1)
            return (sorted[0], sorted[0]);

        if (DnsCanonical.CompareDnsNames(Q, sorted[0]) < 0)
            return (sorted[^1], sorted[0]);

        if (DnsCanonical.CompareDnsNames(Q, sorted[^1]) > 0)
            return (sorted[^1], sorted[0]);

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            if (DnsCanonical.CompareDnsNames(sorted[i], Q) < 0 && DnsCanonical.CompareDnsNames(Q, sorted[i + 1]) < 0)
                return (sorted[i], sorted[i + 1]);
        }

        return (sorted[^1], sorted[0]);
    }

    public static (string owner, string next) GetNsecForOwner(string owner, IReadOnlyList<string> sorted)
    {
        var o = DnsCanonical.NormalizeOwner(owner);
        var idx = -1;
        for (var i = 0; i < sorted.Count; i++)
        {
            if (string.Equals(sorted[i], o, StringComparison.Ordinal))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0)
            idx = 0;

        var next = sorted[(idx + 1) % sorted.Count];
        return (sorted[idx], next);
    }
}
