using System.Collections.Concurrent;

namespace sins.Services;

public sealed class DnssecCatalog : IDnssecCatalog
{
    private readonly ConcurrentDictionary<string, long> _versions = new(StringComparer.Ordinal);

    public long GetVersion(string apex)
    {
        var a = Normalize(apex);
        return _versions.GetValueOrDefault(a, 0);
    }

    public void InvalidateZone(string apex)
    {
        var a = Normalize(apex);
        _versions.AddOrUpdate(a, 1, (_, v) => v + 1);
    }

    private static string Normalize(string apex) =>
        string.IsNullOrWhiteSpace(apex) ? string.Empty : apex.Trim().TrimEnd('.').ToLowerInvariant();
}
