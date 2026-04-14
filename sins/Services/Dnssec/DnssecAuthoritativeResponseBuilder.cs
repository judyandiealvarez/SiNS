using sins.Models;

namespace sins.Services.Dnssec;

public enum DnssecAuthoritativeKind
{
    PositiveAnswer,
    DnskeyQuery,
    NxDomain,
    NoData
}

/// <summary>Builds signed authoritative DNS responses (algorithm 13, NSEC).</summary>
public static class DnssecAuthoritativeResponseBuilder
{
    private const uint DefaultSignatureValiditySeconds = 30 * 24 * 3600u;

    /// <summary>DNSKEY answers without RRSIG/OPT (when the client did not set DO).</summary>
    public static byte[]? BuildUnsignedDnskey(
        byte[] request,
        DnsQueryParseResult query,
        DnssecZone zone,
        DnssecZoneKeyMaterial keys)
    {
        try
        {
            var apex = DnsCanonical.NormalizeOwner(zone.Apex);
            var buf = new List<byte>();
            buf.AddRange(request.AsSpan(0, 12));
            buf.AddRange(request.AsSpan(12, query.OffsetAfterQuestion - 12));

            ushort anCount = 0;
            var ttl = 3600u;
            var usePtr = string.Equals(apex, DnsCanonical.NormalizeOwner(query.QName), StringComparison.Ordinal);
            var nameMode = usePtr ? (ushort)12 : (ushort)0xFFFF;

            var rrZ = DnssecRrSigner.BuildCanonicalRrForSigning(apex, DnsTypes.Dnskey, DnsTypes.ClassIn, ttl,
                keys.ZskDnskeyRdata);
            var rrK = DnssecRrSigner.BuildCanonicalRrForSigning(apex, DnsTypes.Dnskey, DnsTypes.ClassIn, ttl,
                keys.KskDnskeyRdata);
            var orderedCanon = ComparerSort(new[] { rrZ, rrK });
            foreach (var c in orderedCanon)
            {
                var rdata = ReferenceEquals(c, rrZ) ? keys.ZskDnskeyRdata : keys.KskDnskeyRdata;
                DnssecWireFormat.AppendResourceRecord(buf, apex, nameMode, DnsTypes.Dnskey, DnsTypes.ClassIn, ttl,
                    rdata);
                anCount++;
            }

            var packet = buf.ToArray();
            var flags = (ushort)(0x8480 | (query.Flags & 0x0100));
            DnssecWireFormat.PatchHeaderCounts(packet, anCount, 0, 0, flags, setFlags: true);
            packet[0] = request[0];
            packet[1] = request[1];
            return packet;
        }
        catch
        {
            return null;
        }
    }

    public static byte[]? TryBuild(
        byte[] request,
        DnsQueryParseResult query,
        DnssecAuthoritativeKind kind,
        DnsRecord? positiveRecord,
        DnssecZone zone,
        DnssecZoneKeyMaterial keys,
        IReadOnlyList<string> sortedOwners,
        IReadOnlyDictionary<string, HashSet<ushort>> typesByOwner,
        bool isTcp)
    {
        if (!query.DnssecOk) return null;

        var apex = DnsCanonical.NormalizeOwner(zone.Apex);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var inception = (uint)(now - 3600);
        var expiration = (uint)(now + DefaultSignatureValiditySeconds);

        var buf = new List<byte>();
        buf.AddRange(request.AsSpan(0, 12));
        buf.AddRange(request.AsSpan(12, query.OffsetAfterQuestion - 12));

        ushort anCount = 0;
        ushort nsCount = 0;
        ushort arCount = 0;

        try
        {
            switch (kind)
            {
                case DnssecAuthoritativeKind.PositiveAnswer when positiveRecord != null:
                    AppendSignedPositive(buf, query, positiveRecord, keys, apex, inception, expiration, ref anCount);
                    break;
                case DnssecAuthoritativeKind.DnskeyQuery:
                    AppendDnskeyAnswer(buf, query, keys, apex, inception, expiration, ref anCount);
                    break;
                case DnssecAuthoritativeKind.NxDomain:
                    AppendSignedNxDomain(buf, query, keys, apex, sortedOwners, typesByOwner, inception, expiration,
                        ref nsCount);
                    break;
                case DnssecAuthoritativeKind.NoData:
                    AppendSignedNoData(buf, query, keys, apex, sortedOwners, typesByOwner, inception, expiration,
                        ref nsCount);
                    break;
                default:
                    return null;
            }

            var udpSize = (ushort)Math.Min(query.ClientUdpPayloadSize, DnsQueryParser.DefaultServerUdpPayload);
            DnssecWireFormat.AppendOptPseudoRR(buf, udpSize, dnssecOk: true);
            arCount++;

            var packet = buf.ToArray();
            var flags = (ushort)(0x8480 | (query.Flags & 0x0100));
            if (kind == DnssecAuthoritativeKind.NxDomain)
                flags = (ushort)((flags & 0xFFF0) | 0x0003);

            DnssecWireFormat.PatchHeaderCounts(packet, anCount, nsCount, arCount, flags, setFlags: true);

            if (!isTcp && packet.Length > query.ClientUdpPayloadSize)
                DnssecWireFormat.PatchHeaderTc(packet, true);

            packet[0] = request[0];
            packet[1] = request[1];
            return packet;
        }
        catch
        {
            return null;
        }
    }

    private static void AppendSignedPositive(
        List<byte> buf,
        DnsQueryParseResult query,
        DnsRecord record,
        DnssecZoneKeyMaterial keys,
        string apex,
        uint inception,
        uint expiration,
        ref ushort anCount)
    {
        var owner = DnsCanonical.NormalizeOwner(record.Name);
        var ttl = (uint)record.Ttl;
        var rdata = EncodeRdataForRecord(record);
        var qType = TypeFromString(record.Type);
        var usePtr = string.Equals(owner, DnsCanonical.NormalizeOwner(query.QName), StringComparison.Ordinal);
        var nameMode = usePtr ? (ushort)12 : (ushort)0xFFFF;

        DnssecWireFormat.AppendResourceRecord(buf, owner, nameMode, qType, DnsTypes.ClassIn, ttl, rdata);
        anCount++;

        var labels = (byte)Math.Min(byte.MaxValue, DnsCanonical.CountLabels(owner));
        var rrCanon = DnssecRrSigner.BuildCanonicalRrForSigning(owner, qType, DnsTypes.ClassIn, ttl, rdata);
        var concat = DnssecRrSigner.SortAndConcatRrset(new[] { rrCanon });
        var signed = DnssecRrSigner.BuildRrsigSignedData(qType, DnsTypes.AlgorithmEcdsaP256Sha256, labels, ttl,
            expiration, inception, keys.ZskKeyTag, apex, concat);
        var sig = DnssecRrSigner.SignEcdsaP256Sha256(keys.Zsk, signed);
        var rrsig = DnssecWireFormat.EncodeRrsigRdata(qType, DnsTypes.AlgorithmEcdsaP256Sha256, labels, ttl,
            expiration, inception, keys.ZskKeyTag, apex, sig);
        DnssecWireFormat.AppendResourceRecord(buf, owner, nameMode, DnsTypes.Rrsig, DnsTypes.ClassIn, ttl, rrsig);
        anCount++;
    }

    private static void AppendDnskeyAnswer(
        List<byte> buf,
        DnsQueryParseResult query,
        DnssecZoneKeyMaterial keys,
        string apex,
        uint inception,
        uint expiration,
        ref ushort anCount)
    {
        var ttl = 3600u;
        var usePtr = string.Equals(apex, DnsCanonical.NormalizeOwner(query.QName), StringComparison.Ordinal);
        var nameMode = usePtr ? (ushort)12 : (ushort)0xFFFF;

        var rrZ = DnssecRrSigner.BuildCanonicalRrForSigning(apex, DnsTypes.Dnskey, DnsTypes.ClassIn, ttl,
            keys.ZskDnskeyRdata);
        var rrK = DnssecRrSigner.BuildCanonicalRrForSigning(apex, DnsTypes.Dnskey, DnsTypes.ClassIn, ttl,
            keys.KskDnskeyRdata);
        var orderedCanon = ComparerSort(new[] { rrZ, rrK });
        foreach (var c in orderedCanon)
        {
            var rdata = ReferenceEquals(c, rrZ) ? keys.ZskDnskeyRdata : keys.KskDnskeyRdata;
            DnssecWireFormat.AppendResourceRecord(buf, apex, nameMode, DnsTypes.Dnskey, DnsTypes.ClassIn, ttl, rdata);
            anCount++;
        }

        var concat = DnssecRrSigner.SortAndConcatRrset(orderedCanon);
        var labels = (byte)Math.Min(byte.MaxValue, DnsCanonical.CountLabels(apex));
        var signed = DnssecRrSigner.BuildRrsigSignedData(DnsTypes.Dnskey, DnsTypes.AlgorithmEcdsaP256Sha256, labels,
            ttl, expiration, inception, keys.KskKeyTag, apex, concat);
        var sig = DnssecRrSigner.SignEcdsaP256Sha256(keys.Ksk, signed);
        var rrsig = DnssecWireFormat.EncodeRrsigRdata(DnsTypes.Dnskey, DnsTypes.AlgorithmEcdsaP256Sha256, labels, ttl,
            expiration, inception, keys.KskKeyTag, apex, sig);
        DnssecWireFormat.AppendResourceRecord(buf, apex, nameMode, DnsTypes.Rrsig, DnsTypes.ClassIn, ttl, rrsig);
        anCount++;
    }

    private static byte[][] ComparerSort(byte[][] a)
    {
        var copy = (byte[][])a.Clone();
        Array.Sort(copy, (x, y) =>
        {
            var len = Math.Min(x.Length, y.Length);
            for (var i = 0; i < len; i++)
            {
                var c = x[i].CompareTo(y[i]);
                if (c != 0) return c;
            }

            return x.Length.CompareTo(y.Length);
        });
        return copy;
    }

    private static void AppendSignedNxDomain(
        List<byte> buf,
        DnsQueryParseResult query,
        DnssecZoneKeyMaterial keys,
        string apex,
        IReadOnlyList<string> sortedOwners,
        IReadOnlyDictionary<string, HashSet<ushort>> typesByOwner,
        uint inception,
        uint expiration,
        ref ushort nsCount)
    {
        var q = DnsCanonical.NormalizeOwner(query.QName);
        var (nsecOwner, next) = DnssecNsecChain.FindCoveringNsec(q, sortedOwners);
        AppendNsecAndRrsig(buf, query, keys, apex, nsecOwner, next, typesByOwner, inception, expiration, ref nsCount);
    }

    private static void AppendSignedNoData(
        List<byte> buf,
        DnsQueryParseResult query,
        DnssecZoneKeyMaterial keys,
        string apex,
        IReadOnlyList<string> sortedOwners,
        IReadOnlyDictionary<string, HashSet<ushort>> typesByOwner,
        uint inception,
        uint expiration,
        ref ushort nsCount)
    {
        var q = DnsCanonical.NormalizeOwner(query.QName);
        var (nsecOwner, next) = DnssecNsecChain.GetNsecForOwner(q, sortedOwners);
        AppendNsecAndRrsig(buf, query, keys, apex, nsecOwner, next, typesByOwner, inception, expiration, ref nsCount);
    }

    private static void AppendNsecAndRrsig(
        List<byte> buf,
        DnsQueryParseResult query,
        DnssecZoneKeyMaterial keys,
        string apex,
        string nsecOwner,
        string next,
        IReadOnlyDictionary<string, HashSet<ushort>> typesByOwner,
        uint inception,
        uint expiration,
        ref ushort nsCount)
    {
        var ttl = 3600u;
        var types = TypesForOwner(typesByOwner, nsecOwner, apex);
        var nsecRdata = DnssecWireFormat.EncodeNsecRdata(next, types);
        var usePtr = string.Equals(nsecOwner, DnsCanonical.NormalizeOwner(query.QName), StringComparison.Ordinal);
        var nameMode = usePtr ? (ushort)12 : (ushort)0xFFFF;

        DnssecWireFormat.AppendResourceRecord(buf, nsecOwner, nameMode, DnsTypes.Nsec, DnsTypes.ClassIn, ttl,
            nsecRdata);
        nsCount++;

        var labels = (byte)Math.Min(byte.MaxValue, DnsCanonical.CountLabels(nsecOwner));
        var rrCanon = DnssecRrSigner.BuildCanonicalRrForSigning(nsecOwner, DnsTypes.Nsec, DnsTypes.ClassIn, ttl,
            nsecRdata);
        var concat = DnssecRrSigner.SortAndConcatRrset(new[] { rrCanon });
        var signed = DnssecRrSigner.BuildRrsigSignedData(DnsTypes.Nsec, DnsTypes.AlgorithmEcdsaP256Sha256, labels,
            ttl, expiration, inception, keys.ZskKeyTag, apex, concat);
        var sig = DnssecRrSigner.SignEcdsaP256Sha256(keys.Zsk, signed);
        var rrsig = DnssecWireFormat.EncodeRrsigRdata(DnsTypes.Nsec, DnsTypes.AlgorithmEcdsaP256Sha256, labels, ttl,
            expiration, inception, keys.ZskKeyTag, apex, sig);
        DnssecWireFormat.AppendResourceRecord(buf, nsecOwner, nameMode, DnsTypes.Rrsig, DnsTypes.ClassIn, ttl, rrsig);
        nsCount++;
    }

    private static HashSet<ushort> TypesForOwner(
        IReadOnlyDictionary<string, HashSet<ushort>> typesByOwner,
        string owner,
        string apex)
    {
        var o = DnsCanonical.NormalizeOwner(owner);
        var set = new HashSet<ushort>();
        if (typesByOwner.TryGetValue(o, out var t))
            set.UnionWith(t);
        set.Add(DnsTypes.Rrsig);
        set.Add(DnsTypes.Nsec);
        if (string.Equals(o, apex, StringComparison.Ordinal))
            set.Add(DnsTypes.Dnskey);
        return set;
    }

    private static ReadOnlySpan<byte> EncodeRdataForRecord(DnsRecord record)
    {
        return record.Type.ToUpperInvariant() switch
        {
            "A" => DnssecWireFormat.EncodeARecordRdata(record.Value),
            "AAAA" => DnssecWireFormat.EncodeAaaaRecordRdata(record.Value),
            "TXT" => DnssecWireFormat.EncodeTxtRecordRdata(record.Value),
            "CNAME" => DnssecWireFormat.EncodeCnameOrNsTargetRdata(record.Value),
            "NS" => DnssecWireFormat.EncodeCnameOrNsTargetRdata(record.Value),
            "MX" => EncodeMxFlexible(record.Value),
            _ => throw new NotSupportedException($"Signed RR type {record.Type} is not supported.")
        };
    }

    private static byte[] EncodeMxFlexible(string value)
    {
        var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && ushort.TryParse(parts[0], out var pref))
            return DnssecWireFormat.EncodeMxRecordRdata(pref, parts[1].Trim());
        return DnssecWireFormat.EncodeMxRecordRdata(10, value.Trim());
    }

    private static ushort TypeFromString(string type) =>
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
