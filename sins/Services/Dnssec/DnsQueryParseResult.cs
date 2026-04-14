namespace sins.Services.Dnssec;

/// <summary>Parsed DNS query header, question, and EDNS0 (OPT) hints.</summary>
public sealed class DnsQueryParseResult
{
    public required ushort TransactionId { get; init; }
    public required ushort Flags { get; init; }
    public required string QName { get; init; }
    public required ushort QType { get; init; }
    public required ushort QClass { get; init; }
    /// <summary>Byte offset immediately after the question section (first byte of Answer if any).</summary>
    public required int OffsetAfterQuestion { get; init; }
    /// <summary>EDNS DO (DNSSEC OK) bit.</summary>
    public bool DnssecOk { get; init; }
    /// <summary>Client UDP buffer from OPT, or 512 without EDNS.</summary>
    public int ClientUdpPayloadSize { get; init; }
}
