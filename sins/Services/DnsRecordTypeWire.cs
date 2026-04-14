namespace sins.Services;

/// <summary>Maps DNS record type names (and numeric strings) to on-the-wire QTYPE / RTYPE values.</summary>
public static class DnsRecordTypeWire
{
    /// <summary>Returns IANA RR type number, defaulting to A (1) only for empty/unknown non-numeric names.</summary>
    public static ushort ToWireType(string type)
    {
        if (string.IsNullOrEmpty(type)) return 1;
        var t = type.ToUpperInvariant();
        return t switch
        {
            "A" => 1,
            "NS" => 2,
            "CNAME" => 5,
            "SOA" => 6,
            "MX" => 15,
            "TXT" => 16,
            "AAAA" => 28,
            "OPT" => 41,
            "DS" => 43,
            "RRSIG" => 46,
            "NSEC" => 47,
            "DNSKEY" => 48,
            _ => ushort.TryParse(t, System.Globalization.NumberStyles.Integer, null, out var code) ? code : (ushort)1
        };
    }
}
