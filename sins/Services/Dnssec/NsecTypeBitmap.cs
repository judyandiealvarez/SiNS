namespace sins.Services.Dnssec;

public static class NsecTypeBitmap
{
    public static byte[] Encode(IReadOnlyCollection<ushort> types)
    {
        if (types.Count == 0) return Array.Empty<byte>();

        var byWindow = types.GroupBy(t => (byte)(t >> 8)).OrderBy(g => g.Key).ToList();
        var blocks = new List<byte>();
        foreach (var g in byWindow)
        {
            var window = g.Key;
            var lows = g.Select(t => (int)(t & 0xFF)).ToList();
            var maxLow = lows.Max();
            var len = maxLow / 8 + 1;
            if (len > 32) len = 32;
            var map = new byte[len];
            foreach (var low in lows)
            {
                if (low / 8 >= map.Length) continue;
                map[low / 8] |= (byte)(1 << (7 - (low % 8)));
            }

            while (len > 0 && map[len - 1] == 0) len--;

            if (len == 0) continue;

            blocks.Add(window);
            blocks.Add((byte)len);
            blocks.AddRange(map.AsSpan(0, len));
        }

        return blocks.ToArray();
    }
}
