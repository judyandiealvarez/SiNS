using System.Security.Cryptography;
using System.Text;
using sins.Models;

namespace sins.Services.Dnssec;

/// <summary>Loaded KSK/ZSK material for one zone (algorithm 13).</summary>
public sealed class DnssecZoneKeyMaterial : IDisposable
{
    public const ushort KskFlags = 257; // ZONE + SEP
    public const ushort ZskFlags = 256; // ZONE

    public required string Apex { get; init; }
    public required ECDsa Ksk { get; init; }
    public required ECDsa Zsk { get; init; }
    public required byte[] KskDnskeyRdata { get; init; }
    public required byte[] ZskDnskeyRdata { get; init; }
    public ushort KskKeyTag { get; init; }
    public ushort ZskKeyTag { get; init; }

    public static DnssecZoneKeyMaterial Load(DnssecZone zone)
    {
        if (zone.Algorithm != DnsTypes.AlgorithmEcdsaP256Sha256)
            throw new NotSupportedException($"Algorithm {zone.Algorithm} is not supported.");

        var ksk = ECDsa.Create();
        ksk.ImportFromPem(zone.KskPrivateKeyPem);
        var zsk = ECDsa.Create();
        zsk.ImportFromPem(zone.ZskPrivateKeyPem);

        var kskPub = BuildDnskeyRdata(KskFlags, 3, DnsTypes.AlgorithmEcdsaP256Sha256, ksk);
        var zskPub = BuildDnskeyRdata(ZskFlags, 3, DnsTypes.AlgorithmEcdsaP256Sha256, zsk);
        var kskTag = DnssecKeyTag.Compute(kskPub);
        var zskTag = DnssecKeyTag.Compute(zskPub);

        return new DnssecZoneKeyMaterial
        {
            Apex = DnsCanonical.NormalizeOwner(zone.Apex),
            Ksk = ksk,
            Zsk = zsk,
            KskDnskeyRdata = kskPub,
            ZskDnskeyRdata = zskPub,
            KskKeyTag = kskTag,
            ZskKeyTag = zskTag
        };
    }

    private static byte[] BuildDnskeyRdata(ushort flags, byte protocol, byte algorithm, ECDsa key)
    {
        var p = key.ExportParameters(false);
        if (p.Q.X == null || p.Q.Y == null || p.Q.X.Length != 32 || p.Q.Y.Length != 32)
            throw new InvalidOperationException("ECDSA P-256 public key required.");

        var xy = new byte[64];
        p.Q.X.CopyTo(xy, 0);
        p.Q.Y.CopyTo(xy, 32);
        return DnssecWireFormat.EncodeDnskeyRdata(flags, protocol, algorithm, xy);
    }

    /// <summary>DS digest (SHA-256) wire for KSK at delegation point (owner = apex).</summary>
    public byte[] ComputeDsDigestSha256()
    {
        var owner = DnsCanonical.OwnerToWire(Apex);
        using var sha = SHA256.Create();
        var buf = new byte[owner.Length + KskDnskeyRdata.Length];
        owner.AsSpan().CopyTo(buf);
        KskDnskeyRdata.AsSpan().CopyTo(buf.AsSpan(owner.Length));
        return sha.ComputeHash(buf);
    }

    public void Dispose()
    {
        Ksk.Dispose();
        Zsk.Dispose();
    }
}
