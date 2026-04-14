namespace sins.Services;

/// <summary>Invalidation generation for DNSSEC NSEC / key material caches per zone apex.</summary>
public interface IDnssecCatalog
{
    long GetVersion(string apex);

    void InvalidateZone(string apex);
}
