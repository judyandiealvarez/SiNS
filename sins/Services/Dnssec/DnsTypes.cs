namespace sins.Services.Dnssec;

public static class DnsTypes
{
    public const ushort A = 1;
    public const ushort Ns = 2;
    public const ushort Cname = 5;
    public const ushort Soa = 6;
    public const ushort Mx = 15;
    public const ushort Txt = 16;
    public const ushort Rrsig = 46;
    public const ushort Nsec = 47;
    public const ushort Dnskey = 48;
    public const ushort Opt = 41; // EDNS0 pseudo-RR

    public const ushort ClassIn = 1;
    public const ushort OptPseudoClass = 255; // not used for OPT - class is UDP payload size

    public const byte AlgorithmEcdsaP256Sha256 = 13;
    public const byte DigestSha256 = 2;
}
