using k8s;

namespace sins.Services;

public class KubernetesIngressHostResolver : IIngressHostResolver
{
    private readonly ILogger<KubernetesIngressHostResolver> _logger;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(60);
    private DateTime _cacheExpiry = DateTime.MinValue;
    private HashSet<string> _cachedHosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public KubernetesIngressHostResolver(ILogger<KubernetesIngressHostResolver> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsHostInAnyIngressAsync(string dnsName, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeHost(dnsName);
        if (string.IsNullOrEmpty(normalized))
            return false;

        var hosts = await GetIngressHostsAsync(cancellationToken);
        return hosts.Contains(normalized);
    }

    private static string NormalizeHost(string dnsName)
    {
        if (string.IsNullOrWhiteSpace(dnsName))
            return string.Empty;
        var s = dnsName.TrimEnd('.');
        return string.IsNullOrEmpty(s) ? string.Empty : s.ToLowerInvariant();
    }

    private async Task<HashSet<string>> GetIngressHostsAsync(CancellationToken cancellationToken)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (DateTime.UtcNow < _cacheExpiry)
                return _cachedHosts;

            var hosts = await FetchIngressHostsFromClusterAsync(cancellationToken);
            _cachedHosts = hosts;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheTtl);
            return hosts;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task<HashSet<string>> FetchIngressHostsFromClusterAsync(CancellationToken cancellationToken)
    {
        KubernetesClientConfiguration config;
        try
        {
            config = KubernetesClientConfiguration.InClusterConfig();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Not running in Kubernetes cluster; ingress host lookup disabled");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var client = new Kubernetes(config);
            var response = await client.NetworkingV1.ListIngressForAllNamespacesAsync(cancellationToken: cancellationToken);
            var list = response?.Items ?? new List<k8s.Models.V1Ingress>();
            var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ingress in list)
            {
                var spec = ingress.Spec;
                if (spec?.Rules == null)
                    continue;
                foreach (var rule in spec.Rules)
                {
                    if (!string.IsNullOrEmpty(rule.Host))
                        hosts.Add(rule.Host.TrimEnd('.').ToLowerInvariant());
                }
            }

            _logger.LogDebug("Resolved {Count} ingress hosts from cluster", hosts.Count);
            return hosts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list Ingress resources; ingress host lookup disabled for this period");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
