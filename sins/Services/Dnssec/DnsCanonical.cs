using System.Text;
using sins.Models;

namespace sins.Services.Dnssec;

/// <summary>RFC 4034 canonical DNS name ordering and owner normalization.</summary>
public static class DnsCanonical
{
    public static string NormalizeOwner(string fqdn)
    {
        if (string.IsNullOrWhiteSpace(fqdn)) return string.Empty;
        return fqdn.Trim().TrimEnd('.').ToLowerInvariant();
    }

    /// <summary>Lexicographic order on DNS names (wire-style label order).</summary>
    public static int CompareDnsNames(string a, string b)
    {
        var la = SplitLabels(a);
        var lb = SplitLabels(b);
        var n = Math.Max(la.Count, lb.Count);
        for (var i = 0; i < n; i++)
        {
            var ca = i < la.Count ? la[la.Count - 1 - i] : string.Empty;
            var cb = i < lb.Count ? lb[lb.Count - 1 - i] : string.Empty;
            var c = string.CompareOrdinal(ca, cb);
            if (c != 0) return c;
        }

        return 0;
    }

    public static void SortNames(List<string> names)
    {
        names.Sort(CompareDnsNames);
    }

    public static List<string> SplitLabels(string fqdn)
    {
        var n = NormalizeOwner(fqdn);
        if (n.Length == 0) return new List<string>();
        return n.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public static int CountLabels(string fqdn)
    {
        var labels = SplitLabels(fqdn);
        return labels.Count;
    }

    /// <summary>Wire-format owner name: lowercase labels, uncompressed.</summary>
    public static void WriteOwnerName(List<byte> buf, string fqdn)
    {
        var n = NormalizeOwner(fqdn);
        if (n.Length == 0)
        {
            buf.Add(0);
            return;
        }

        foreach (var label in n.Split('.'))
        {
            var ascii = Encoding.ASCII.GetBytes(label);
            if (ascii.Length > 63) throw new InvalidOperationException("DNS label too long.");
            buf.Add((byte)ascii.Length);
            buf.AddRange(ascii);
        }

        buf.Add(0);
    }

    public static byte[] OwnerToWire(string fqdn)
    {
        var buf = new List<byte>();
        WriteOwnerName(buf, fqdn);
        return buf.ToArray();
    }

    public static bool NameUnderApex(string name, string apex)
    {
        var n = NormalizeOwner(name);
        var a = NormalizeOwner(apex);
        if (a.Length == 0) return false;
        return n == a || n.EndsWith("." + a, StringComparison.Ordinal);
    }

    /// <summary>Longest enabled dnssec apex covering <paramref name="name"/>.</summary>
    public static DnssecZone? FindCoveringZone(string name, IReadOnlyList<DnssecZone> zones)
    {
        DnssecZone? best = null;
        var bestLen = -1;
        foreach (var z in zones)
        {
            if (!z.Enabled) continue;
            var a = NormalizeOwner(z.Apex);
            if (a.Length == 0) continue;
            if (!NameUnderApex(name, a)) continue;
            if (a.Length > bestLen)
            {
                bestLen = a.Length;
                best = z;
            }
        }

        return best;
    }
}
