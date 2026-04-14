using System.Buffers.Binary;
using System.Text;

namespace sins.Services.Dnssec;

internal static class DnssecWireFormat
{
    public static void WriteUInt16(List<byte> buf, ushort v)
    {
        buf.Add((byte)(v >> 8));
        buf.Add((byte)(v & 0xFF));
    }

    public static void WriteUInt32(List<byte> buf, uint v)
    {
        buf.Add((byte)(v >> 24));
        buf.Add((byte)(v >> 16));
        buf.Add((byte)(v >> 8));
        buf.Add((byte)(v & 0xFF));
    }

    public static void WriteNamePointer(List<byte> buf, ushort offset)
    {
        buf.Add((byte)(0xC0 | (offset >> 8)));
        buf.Add((byte)(offset & 0xFF));
    }

    /// <summary>Append RR header + RDATA; <paramref name="nameMode"/> 0xFFFF = uncompressed from <paramref name="owner"/>; else pointer offset.</summary>
    public static void AppendResourceRecord(
        List<byte> buf,
        string owner,
        ushort namePtrOrSentinel,
        ushort type,
        ushort @class,
        uint ttl,
        ReadOnlySpan<byte> rdata)
    {
        if (namePtrOrSentinel == 0xFFFF)
            DnsCanonical.WriteOwnerName(buf, owner);
        else
            WriteNamePointer(buf, namePtrOrSentinel);

        WriteUInt16(buf, type);
        WriteUInt16(buf, @class);
        WriteUInt32(buf, ttl);
        WriteUInt16(buf, (ushort)rdata.Length);
        buf.AddRange(rdata);
    }

    public static byte[] EncodeARecordRdata(string ipv4)
    {
        var parts = ipv4.Split('.');
        if (parts.Length != 4) throw new FormatException("Invalid IPv4.");
        return new[]
        {
            byte.Parse(parts[0]),
            byte.Parse(parts[1]),
            byte.Parse(parts[2]),
            byte.Parse(parts[3])
        };
    }

    public static byte[] EncodeAaaaRecordRdata(string ipv6)
    {
        if (!System.Net.IPAddress.TryParse(ipv6, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
            throw new FormatException("Invalid IPv6.");
        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 16) throw new FormatException("IPv6 must be 16 bytes.");
        return bytes;
    }

    public static byte[] EncodeTxtRecordRdata(string text)
    {
        var enc = Encoding.UTF8.GetBytes(text);
        var buf = new List<byte>();
        for (var i = 0; i < enc.Length; i += 255)
        {
            var len = Math.Min(255, enc.Length - i);
            buf.Add((byte)len);
            buf.AddRange(enc.AsSpan(i, len));
        }

        if (buf.Count == 0)
        {
            buf.Add(0);
        }

        return buf.ToArray();
    }

    public static byte[] EncodeCnameOrNsTargetRdata(string target)
    {
        var buf = new List<byte>();
        DnsCanonical.WriteOwnerName(buf, target);
        return buf.ToArray();
    }

    public static byte[] EncodeMxRecordRdata(ushort preference, string exchange)
    {
        var buf = new List<byte>();
        WriteUInt16(buf, preference);
        DnsCanonical.WriteOwnerName(buf, exchange);
        return buf.ToArray();
    }

    public static byte[] EncodeDnskeyRdata(ushort flags, byte protocol, byte algorithm, ReadOnlySpan<byte> xy64)
    {
        if (xy64.Length != 64) throw new ArgumentException("EC P-256 public key must be 64 bytes (X|Y).");
        var buf = new byte[2 + 1 + 1 + 64];
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(0, 2), flags);
        buf[2] = protocol;
        buf[3] = algorithm;
        xy64.CopyTo(buf.AsSpan(4));
        return buf;
    }

    public static byte[] EncodeNsecRdata(string nextDomainName, IReadOnlyCollection<ushort> types)
    {
        var next = new List<byte>();
        DnsCanonical.WriteOwnerName(next, nextDomainName);
        var bitmap = NsecTypeBitmap.Encode(types);
        var r = new byte[next.Count + bitmap.Length];
        next.CopyTo(r, 0);
        bitmap.CopyTo(r.AsSpan(next.Count));
        return r;
    }

    public static byte[] EncodeRrsigRdata(
        ushort typeCovered,
        byte algorithm,
        byte labels,
        uint originalTtl,
        uint signatureExpiration,
        uint signatureInception,
        ushort keyTag,
        string signerName,
        ReadOnlySpan<byte> signature)
    {
        var signer = new List<byte>();
        DnsCanonical.WriteOwnerName(signer, signerName);
        var headerLen = 2 + 1 + 1 + 4 + 4 + 4 + 2 + signer.Count;
        var buf = new byte[headerLen + signature.Length];
        var o = 0;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(o, 2), typeCovered);
        o += 2;
        buf[o++] = algorithm;
        buf[o++] = labels;
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(o, 4), originalTtl);
        o += 4;
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(o, 4), signatureExpiration);
        o += 4;
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(o, 4), signatureInception);
        o += 4;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(o, 2), keyTag);
        o += 2;
        signer.CopyTo(buf, o);
        o += signer.Count;
        signature.CopyTo(buf.AsSpan(o));
        return buf;
    }

    public static void AppendOptPseudoRR(List<byte> buf, ushort udpPayload, bool dnssecOk)
    {
        buf.Add(0); // root owner
        WriteUInt16(buf, DnsTypes.Opt);
        WriteUInt16(buf, udpPayload);
        var flags = dnssecOk ? 0x8000u : 0u;
        var ttlField = (0u << 24) | (0u << 16) | flags;
        WriteUInt32(buf, ttlField);
        WriteUInt16(buf, 0); // RDLEN
    }

    public static void PatchHeaderCounts(byte[] packet, ushort anCount, ushort nsCount, ushort arCount, ushort flagsOrMask, bool setFlags)
    {
        if (packet.Length < 12) return;
        if (setFlags)
        {
            packet[2] = (byte)(flagsOrMask >> 8);
            packet[3] = (byte)(flagsOrMask & 0xFF);
        }

        packet[6] = (byte)(anCount >> 8);
        packet[7] = (byte)(anCount & 0xFF);
        packet[8] = (byte)(nsCount >> 8);
        packet[9] = (byte)(nsCount & 0xFF);
        packet[10] = (byte)(arCount >> 8);
        packet[11] = (byte)(arCount & 0xFF);
    }

    public static void PatchHeaderTc(byte[] packet, bool tc)
    {
        if (packet.Length < 3) return;
        if (tc)
            packet[2] |= 0x02;
        else
            packet[2] &= unchecked((byte)~0x02);
    }
}
