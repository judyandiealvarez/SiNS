using System.Text;

namespace sins.Services.Dnssec;

/// <summary>Parses DNS query packets: one question, optional Additional OPT (EDNS0).</summary>
public static class DnsQueryParser
{
    public const int DefaultUdpPayload = 512;
    public const int DefaultServerUdpPayload = 1232;

    /// <summary>Robust parse used by the DNS server: recomputes question end from skipped sections.</summary>
    public static bool TryParseDetailed(byte[] data, out DnsQueryParseResult result)
    {
        result = null!;
        if (data.Length < 12) return false;

        var id = (ushort)((data[0] << 8) | data[1]);
        var flags = (ushort)((data[2] << 8) | data[3]);
        if ((flags & 0x8000) != 0) return false;

        var qdCount = (ushort)((data[4] << 8) | data[5]);
        var anCount = (ushort)((data[6] << 8) | data[7]);
        var nsCount = (ushort)((data[8] << 8) | data[9]);
        var arCount = (ushort)((data[10] << 8) | data[11]);
        if (qdCount != 1) return false;

        var pos = 12;
        if (!TryReadName(data, ref pos, out var qName)) return false;
        if (pos + 4 > data.Length) return false;

        var qType = (ushort)((data[pos] << 8) | data[pos + 1]);
        var qClass = (ushort)((data[pos + 2] << 8) | data[pos + 3]);
        pos += 4;
        if (qClass != DnsTypes.ClassIn) return false;

        var offsetAfterQuestion = pos;

        var dnssecOk = false;
        var clientUdp = DefaultUdpPayload;

        for (var i = 0; i < anCount; i++)
        {
            if (!SkipResourceRecord(data, ref pos)) return false;
        }

        for (var i = 0; i < nsCount; i++)
        {
            if (!SkipResourceRecord(data, ref pos)) return false;
        }

        for (var i = 0; i < arCount; i++)
        {
            if (!TryReadName(data, ref pos, out _)) return false;
            if (pos + 10 > data.Length) return false;
            var rType = (ushort)((data[pos] << 8) | data[pos + 1]);
            var rClass = (ushort)((data[pos + 2] << 8) | data[pos + 3]);
            pos += 4;
            var ttlOrExt = (uint)((data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3]);
            pos += 4;
            var rdLength = (ushort)((data[pos] << 8) | data[pos + 1]);
            pos += 2;
            if (pos + rdLength > data.Length) return false;

            if (rType == DnsTypes.Opt)
            {
                clientUdp = rClass == 0 ? DefaultUdpPayload : rClass;
                var optFlags = (ushort)(ttlOrExt & 0xFFFF);
                dnssecOk = (optFlags & 0x8000) != 0;
            }

            pos += rdLength;
        }

        result = new DnsQueryParseResult
        {
            TransactionId = id,
            Flags = flags,
            QName = qName,
            QType = qType,
            QClass = qClass,
            OffsetAfterQuestion = offsetAfterQuestion,
            DnssecOk = dnssecOk,
            ClientUdpPayloadSize = Math.Clamp(clientUdp, 512, 4096)
        };
        return true;
    }

    public static bool TryReadName(byte[] data, ref int offset, out string name)
    {
        name = string.Empty;
        var jumped = false;
        var jumpBack = 0;
        var labels = new List<string>();
        var steps = 0;
        var pos = offset;

        while (steps++ < 128)
        {
            if (pos >= data.Length) return false;
            var len = data[pos];

            if (len == 0)
            {
                pos++;
                if (!jumped) offset = pos;
                else offset = jumpBack;
                name = string.Join('.', labels);
                return true;
            }

            if ((len & 0xC0) == 0xC0)
            {
                if (pos + 1 >= data.Length) return false;
                var ptr = ((len & 0x3F) << 8) | data[pos + 1];
                if (ptr >= data.Length) return false;
                if (!jumped)
                {
                    jumpBack = pos + 2;
                    jumped = true;
                }

                pos = ptr;
                continue;
            }

            if ((len & 0xC0) != 0) return false;

            pos++;
            if (pos + len > data.Length) return false;
            labels.Add(Encoding.ASCII.GetString(data, pos, len));
            pos += len;
        }

        return false;
    }

    private static bool SkipResourceRecord(byte[] data, ref int pos)
    {
        if (!TryReadName(data, ref pos, out _)) return false;
        if (pos + 10 > data.Length) return false;
        pos += 8; // type class ttl
        var rdLength = (ushort)((data[pos] << 8) | data[pos + 1]);
        pos += 2;
        if (pos + rdLength > data.Length) return false;
        pos += rdLength;
        return true;
    }
}
