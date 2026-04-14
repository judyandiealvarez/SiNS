namespace sins.Services.Dnssec;

public static class DnssecKeyTag
{
    public static ushort Compute(ReadOnlySpan<byte> dnskeyRdata)
    {
        uint ac = 0;
        for (var i = 0; i < dnskeyRdata.Length; i++)
        {
            ac += (i & 1) != 0 ? dnskeyRdata[i] : (uint)dnskeyRdata[i] << 8;
        }

        ac += ac >> 16 & 0xFFFF;
        return (ushort)(ac & 0xFFFF);
    }
}
