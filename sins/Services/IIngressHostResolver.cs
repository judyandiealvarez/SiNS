namespace sins.Services;

public interface IIngressHostResolver
{
    /// <summary>
    /// Returns true if the given DNS name (host) appears as a host in any Ingress in any namespace.
    /// </summary>
    Task<bool> IsHostInAnyIngressAsync(string dnsName, CancellationToken cancellationToken = default);
}
