using System.Buffers.Binary;
using System.Security.Cryptography;

namespace sins.Services.Dnssec;

internal static class DnssecRrSigner
{
    private sealed class ByteComparer : IComparer<byte[]>
    {
        public int Compare(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var len = Math.Min(x.Length, y.Length);
            for (var i = 0; i < len; i++)
            {
                var c = x[i].CompareTo(y[i]);
                if (c != 0) return c;
            }

            return x.Length.CompareTo(y.Length);
        }
    }

    private static readonly ByteComparer Comparer = new();

    /// <summary>RR wire for signing: owner + type + class + original TTL + RDLENGTH + RDATA (RFC 4034).</summary>
    public static byte[] BuildCanonicalRrForSigning(string owner, ushort type, ushort @class, uint originalTtl, ReadOnlySpan<byte> rdata)
    {
        var own = new List<byte>();
        DnsCanonical.WriteOwnerName(own, owner);
        var buf = new byte[own.Count + 2 + 2 + 4 + 2 + rdata.Length];
        var o = 0;
        own.CopyTo(buf, o);
        o += own.Count;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(o, 2), type);
        o += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(o, 2), @class);
        o += 2;
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(o, 4), originalTtl);
        o += 4;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(o, 2), (ushort)rdata.Length);
        o += 2;
        rdata.CopyTo(buf.AsSpan(o));
        return buf;
    }

    public static byte[] SortAndConcatRrset(IReadOnlyList<byte[]> canonicalRrs)
    {
        var arr = canonicalRrs.ToArray();
        Array.Sort(arr, Comparer);
        return arr.SelectMany(a => a).ToArray();
    }

    public static byte[] BuildRrsigSignedData(
        ushort typeCovered,
        byte algorithm,
        byte labels,
        uint originalTtl,
        uint expiration,
        uint inception,
        ushort keyTag,
        string signerName,
        ReadOnlySpan<byte> canonicalRrsetConcat)
    {
        var signer = new List<byte>();
        DnsCanonical.WriteOwnerName(signer, signerName);
        var header = new byte[2 + 1 + 1 + 4 + 4 + 4 + 2 + signer.Count];
        var o = 0;
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(o, 2), typeCovered);
        o += 2;
        header[o++] = algorithm;
        header[o++] = labels;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(o, 4), originalTtl);
        o += 4;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(o, 4), expiration);
        o += 4;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(o, 4), inception);
        o += 4;
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(o, 2), keyTag);
        o += 2;
        signer.CopyTo(header, o);
        o += signer.Count;
        var total = new byte[o + canonicalRrsetConcat.Length];
        header.AsSpan(0, o).CopyTo(total);
        canonicalRrsetConcat.CopyTo(total.AsSpan(o));
        return total;
    }

    public static byte[] SignEcdsaP256Sha256(ECDsa key, byte[] signedData)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(signedData);
        return key.SignHash(hash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
}
