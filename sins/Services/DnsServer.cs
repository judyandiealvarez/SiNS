using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sins.Data;
using sins.Models;
using sins.Services.Dnssec;

namespace sins.Services;

public class DnsServer : BackgroundService
{
    private readonly ILogger<DnsServer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IIngressHostResolver _ingressHostResolver;
    private readonly UdpClient _udpClient;
    private readonly TcpListener _tcpListener;
    private int _udpPort;
    private int _tcpPort;
    private string[] _upstreamServers;
    private string? _haproxyIp;
    private List<(string Domain, string UpstreamServer)> _domainUpstreamMappings = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public DnsServer(ILogger<DnsServer> logger, IServiceProvider serviceProvider, IIngressHostResolver ingressHostResolver)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _ingressHostResolver = ingressHostResolver;

        // Initialize with default values, will be updated in ExecuteAsync
        _udpPort = 53;
        _tcpPort = 53;
        _upstreamServers = new[] { "8.8.8.8", "1.1.1.1", "2001:4860:4860::8888", "2606:4700:4700::1111" };

        _udpClient = new UdpClient();
        _tcpListener = new TcpListener(IPAddress.Any, _tcpPort);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Load configuration from database
            await LoadConfigurationAsync();

            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));

            _tcpListener.Start();

            _logger.LogInformation("DNS Server started on UDP port {UdpPort} and TCP port {TcpPort}", _udpPort, _tcpPort);

            var udpTask = HandleUdpRequestsAsync(stoppingToken);
            var tcpTask = HandleTcpRequestsAsync(stoppingToken);
            var cacheCleanupTask = CleanupExpiredCacheAsync(stoppingToken);
            var configReloadTask = ReloadConfigurationAsync(stoppingToken);

            await Task.WhenAll(udpTask, tcpTask, cacheCleanupTask, configReloadTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DNS server");
        }
        finally
        {
            _udpClient?.Close();
            _tcpListener?.Stop();
        }
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

            _udpPort = await config.GetIntValueAsync("UdpPort", 53);
            _tcpPort = await config.GetIntValueAsync("TcpPort", 53);
            _upstreamServers = await config.GetStringArrayValueAsync("UpstreamServers", new[] { "8.8.8.8", "1.1.1.1", "2001:4860:4860::8888", "2606:4700:4700::1111" });
            var haproxy = await config.GetValueAsync("Haproxy", null);
            _haproxyIp = string.IsNullOrWhiteSpace(haproxy) ? null : haproxy.Trim();

            var context = scope.ServiceProvider.GetRequiredService<DnsContext>();
            var list = await context.DomainUpstreamMappings.AsNoTracking().ToListAsync();
            _domainUpstreamMappings = list
                .Select(m => (Domain: NormalizeDomain(m.Domain), UpstreamServer: m.UpstreamServer.Trim()))
                .Where(m => !string.IsNullOrEmpty(m.Domain) && !string.IsNullOrEmpty(m.UpstreamServer))
                .OrderByDescending(m => m.Domain.Length)
                .ToList();

            _logger.LogInformation("Configuration loaded - UDP: {UdpPort}, TCP: {TcpPort}, Upstream: {UpstreamServers}, Haproxy: {Haproxy}, Domain mappings: {MappingCount}",
                _udpPort, _tcpPort, string.Join(", ", _upstreamServers), _haproxyIp ?? "(none)", _domainUpstreamMappings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not load configuration from database, using defaults: {Message}", ex.Message);
            _udpPort = 53;
            _tcpPort = 53;
            _upstreamServers = new[] { "8.8.8.8", "1.1.1.1", "2001:4860:4860::8888", "2606:4700:4700::1111" };
            _haproxyIp = null;
            _domainUpstreamMappings = new List<(string Domain, string UpstreamServer)>();
        }
    }

    private static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return string.Empty;
        return domain.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private string[] GetUpstreamServersForName(string dnsName)
    {
        var name = NormalizeDomain(dnsName);
        if (string.IsNullOrEmpty(name)) return _upstreamServers;

        foreach (var (domain, upstream) in _domainUpstreamMappings)
        {
            if (name == domain || name.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { upstream };
            }
        }

        return _upstreamServers;
    }

    private async Task ReloadConfigurationAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Reload every minute
                await LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading configuration");
            }
        }
    }

    private async Task HandleUdpRequestsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(stoppingToken);
                _ = Task.Run(async () => await HandleUdpRequestAsync(result.Buffer, result.RemoteEndPoint, stoppingToken), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error handling UDP request");
            }
        }
    }

    private async Task HandleUdpRequestAsync(byte[] request, EndPoint remoteEndPoint, CancellationToken stoppingToken)
    {
        try
        {
            var response = await ProcessDnsRequestAsync(request, remoteEndPoint, false, stoppingToken);
            if (response != null)
            {
                await _udpClient.SendAsync(response, response.Length, (IPEndPoint)remoteEndPoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UDP request from {RemoteEndPoint}", remoteEndPoint);
        }
    }

    private async Task HandleTcpRequestsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
                _ = Task.Run(async () => await HandleTcpClientAsync(client, stoppingToken), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error handling TCP request");
            }
        }
    }

    private async Task HandleTcpClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[2];

                // Read length prefix
                await stream.ReadAsync(buffer, 0, 2, stoppingToken);
                var length = (buffer[0] << 8) | buffer[1];

                // Read DNS message
                var dnsMessage = new byte[length];
                await stream.ReadAsync(dnsMessage, 0, length, stoppingToken);

                var response = await ProcessDnsRequestAsync(dnsMessage, client.Client.RemoteEndPoint!, true, stoppingToken);

                if (response != null)
                {
                    // Write length prefix
                    var responseLength = (ushort)response.Length;
                    var lengthBytes = new byte[] { (byte)(responseLength >> 8), (byte)(responseLength & 0xFF) };
                    await stream.WriteAsync(lengthBytes, 0, 2, stoppingToken);

                    // Write response
                    await stream.WriteAsync(response, 0, response.Length, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling TCP client");
        }
    }

    private async Task<byte[]?> ProcessDnsRequestAsync(byte[] request, EndPoint remoteEndPoint, bool isTcp, CancellationToken stoppingToken)
    {
        try
        {
            DnsQueryParseResult? pq = null;
            if (DnsQueryParser.TryParseDetailed(request, out var parsed))
                pq = parsed;

            var dnsMessage = pq != null
                ? new DnsMessage
                {
                    TransactionId = pq.TransactionId,
                    Name = pq.QName,
                    Type = GetDnsTypeString(pq.QType)
                }
                : ParseDnsMessage(request);
            if (dnsMessage == null) return null;

            if (pq == null)
            {
                pq = new DnsQueryParseResult
                {
                    TransactionId = dnsMessage.TransactionId,
                    Flags = 0,
                    QName = dnsMessage.Name,
                    QType = GetDnsTypeUshort(dnsMessage.Type),
                    QClass = DnsTypes.ClassIn,
                    OffsetAfterQuestion = ComputeOffsetAfterQuestion(request),
                    DnssecOk = false,
                    ClientUdpPayloadSize = DnsQueryParser.DefaultUdpPayload
                };
            }

            _logger.LogInformation("DNS request from {RemoteEndPoint}: {Name} ({Type})", remoteEndPoint, dnsMessage.Name, dnsMessage.Type);

            // Check cache first
            var cachedResponse = await GetCachedResponseAsync(dnsMessage.Name, dnsMessage.Type);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for {Name} ({Type})", dnsMessage.Name, dnsMessage.Type);
                return CreateResponseFromCache(request, cachedResponse);
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DnsContext>();
            var signedZones = await context.DnssecZones.AsNoTracking().Where(z => z.Enabled).ToListAsync(stoppingToken);
            var coveringZone = DnsCanonical.FindCoveringZone(dnsMessage.Name, signedZones);
            var wantDnssec = coveringZone != null && pq.DnssecOk;

            async Task<byte[]?> TrySignedAsync(DnssecAuthoritativeKind kind, DnsRecord? record)
            {
                if (!wantDnssec) return null;
                try
                {
                    using var mat = DnssecZoneKeyMaterial.Load(coveringZone!);
                    var load = await DnssecZoneDataLoader.LoadAsync(context, coveringZone!.Apex, stoppingToken);
                    return DnssecAuthoritativeResponseBuilder.TryBuild(request, pq, kind, record, coveringZone, mat,
                        load.SortedOwners, load.TypesByOwner, isTcp);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DNSSEC signing failed for {Name}", dnsMessage.Name);
                    return null;
                }
            }

            // If Haproxy is set and query is A, check Kubernetes Ingress hosts and return Haproxy IP if matched
            if (!string.IsNullOrEmpty(_haproxyIp) && dnsMessage.Type == "A" &&
                IPAddress.TryParse(_haproxyIp, out var haproxyAddr) && haproxyAddr.AddressFamily == AddressFamily.InterNetwork)
            {
                var inIngress = await _ingressHostResolver.IsHostInAnyIngressAsync(dnsMessage.Name, stoppingToken);
                if (inIngress)
                {
                    _logger.LogInformation("Ingress match for {Name} -> Haproxy {HaproxyIp}", dnsMessage.Name, _haproxyIp);
                    var syntheticRecord = new DnsRecord
                    {
                        Name = dnsMessage.Name,
                        Type = "A",
                        Value = _haproxyIp,
                        Ttl = 60
                    };
                    var signedIngress = await TrySignedAsync(DnssecAuthoritativeKind.PositiveAnswer, syntheticRecord);
                    if (signedIngress != null) return signedIngress;
                    return CreateAuthoritativeResponse(request, dnsMessage, syntheticRecord);
                }
            }

            var apex = coveringZone != null ? DnsCanonical.NormalizeOwner(coveringZone.Apex) : string.Empty;
            if (coveringZone != null && pq.QType == DnsTypes.Dnskey &&
                string.Equals(DnsCanonical.NormalizeOwner(dnsMessage.Name), apex, StringComparison.Ordinal))
            {
                var dnskeyAns = await TrySignedAsync(DnssecAuthoritativeKind.DnskeyQuery, null);
                if (dnskeyAns != null)
                {
                    _logger.LogInformation("Authoritative DNSKEY (signed) for {Name}", dnsMessage.Name);
                    return dnskeyAns;
                }

                try
                {
                    using var mat = DnssecZoneKeyMaterial.Load(coveringZone);
                    var unsignedKey = DnssecAuthoritativeResponseBuilder.BuildUnsignedDnskey(request, pq, coveringZone,
                        mat);
                    if (unsignedKey != null)
                    {
                        _logger.LogInformation("Authoritative DNSKEY (unsigned) for {Name}", dnsMessage.Name);
                        return unsignedKey;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DNSKEY response failed for {Name}", dnsMessage.Name);
                }
            }

            // Check authoritative records
            var authoritativeResponse = await GetAuthoritativeResponseAsync(dnsMessage, context);
            if (authoritativeResponse != null)
            {
                _logger.LogInformation("Authoritative response for {Name} ({Type})", dnsMessage.Name, dnsMessage.Type);
                var signed = await TrySignedAsync(DnssecAuthoritativeKind.PositiveAnswer, authoritativeResponse);
                if (signed != null) return signed;
                return CreateAuthoritativeResponse(request, dnsMessage, authoritativeResponse);
            }

            if (coveringZone != null && DnsCanonical.NameUnderApex(dnsMessage.Name, coveringZone.Apex))
            {
                var hasName = await context.DnsRecords.AsNoTracking()
                    .AnyAsync(r => r.Name == dnsMessage.Name, stoppingToken);
                if (!hasName)
                {
                    var nx = await TrySignedAsync(DnssecAuthoritativeKind.NxDomain, null);
                    if (nx != null)
                    {
                        _logger.LogInformation("Authoritative signed NXDOMAIN for {Name}", dnsMessage.Name);
                        return nx;
                    }

                    return CreateNxDomainResponse(request);
                }

                var nodata = await TrySignedAsync(DnssecAuthoritativeKind.NoData, null);
                if (nodata != null)
                {
                    _logger.LogInformation("Authoritative signed NODATA for {Name} ({Type})", dnsMessage.Name, dnsMessage.Type);
                    return nodata;
                }

                return CreateNoDataResponse(request);
            }

            // Query upstream servers
            var (upstreamResponse, upstreamServer) = await QueryUpstreamServersAsync(dnsMessage, stoppingToken);
            if (upstreamResponse != null)
            {
                _logger.LogInformation("Upstream response for {Name} ({Type}) from {Server}", dnsMessage.Name, dnsMessage.Type, upstreamServer);
                await CacheResponseAsync(dnsMessage.Name, dnsMessage.Type, upstreamResponse, upstreamServer, stoppingToken);
                return upstreamResponse;
            }

            // Return NXDOMAIN response
            return CreateNxDomainResponse(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DNS request");
            return CreateErrorResponse(request);
        }
    }

    private static int ComputeOffsetAfterQuestion(byte[] data)
    {
        if (data.Length < 16) return data.Length;
        var pos = 12;
        while (pos < data.Length)
        {
            var len = data[pos];
            if (len == 0)
            {
                pos++;
                break;
            }

            if ((len & 0xC0) == 0xC0)
            {
                pos += 2;
                break;
            }

            pos++;
            if (pos + len > data.Length) return data.Length;
            pos += len;
        }

        return pos + 4 <= data.Length ? pos + 4 : data.Length;
    }

    private static ushort GetDnsTypeUshort(string type) =>
        type.ToUpperInvariant() switch
        {
            "A" => DnsTypes.A,
            "NS" => DnsTypes.Ns,
            "CNAME" => DnsTypes.Cname,
            "SOA" => DnsTypes.Soa,
            "MX" => DnsTypes.Mx,
            "TXT" => DnsTypes.Txt,
            "AAAA" => 28,
            "DNSKEY" => DnsTypes.Dnskey,
            _ => ushort.TryParse(type, out var n) ? n : (ushort)0
        };

    private async Task<byte[]?> GetCachedResponseAsync(string name, string type)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DnsContext>();

        await _cacheLock.WaitAsync();
        try
        {
            var cacheRecord = await context.CacheRecords
                .FirstOrDefaultAsync(c => c.Name == name && c.Type == type && c.ExpiresAt > DateTime.UtcNow);

            if (cacheRecord != null)
            {
                return Convert.FromBase64String(cacheRecord.Response);
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        return null;
    }

    private static async Task<DnsRecord?> GetAuthoritativeResponseAsync(DnsMessage dnsMessage, DnsContext context)
    {
        return await context.DnsRecords
            .FirstOrDefaultAsync(r => r.Name == dnsMessage.Name && r.Type == dnsMessage.Type);
    }

    private async Task<(byte[]? response, string? server)> QueryUpstreamServersAsync(DnsMessage dnsMessage, CancellationToken stoppingToken)
    {
        var serversToUse = GetUpstreamServersForName(dnsMessage.Name);
        foreach (var upstreamServer in serversToUse)
        {
            try
            {
                var response = await QueryUpstreamServerAsync(upstreamServer, dnsMessage, stoppingToken);
                if (response != null)
                {
                    // Modify the response to use the original transaction ID
                    return (ModifyResponseTransactionId(response, dnsMessage.TransactionId), upstreamServer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query upstream server {Server}", upstreamServer);
            }
        }

        return (null, null);
    }

    private async Task<byte[]?> QueryUpstreamServerAsync(string server, DnsMessage dnsMessage, CancellationToken stoppingToken)
    {
        using var client = new UdpClient();
        client.Client.ReceiveTimeout = 5000;
        client.Client.SendTimeout = 5000;

        // Handle both IPv4 and IPv6 addresses
        if (!IPAddress.TryParse(server, out IPAddress? ipAddress) || ipAddress == null)
        {
            _logger.LogWarning("Invalid IP address format: {Server}", server);
            return null;
        }

        var endpoint = new IPEndPoint(ipAddress, 53);

        var request = CreateDnsRequest(dnsMessage);
        await client.SendAsync(request, request.Length, endpoint);

        var response = await client.ReceiveAsync(stoppingToken);
        return response.Buffer;
    }

    private byte[] ModifyResponseTransactionId(byte[] response, ushort transactionId)
    {
        if (response.Length >= 2)
        {
            var modifiedResponse = (byte[])response.Clone();
            modifiedResponse[0] = (byte)(transactionId >> 8);
            modifiedResponse[1] = (byte)(transactionId & 0xFF);
            return modifiedResponse;
        }
        return response;
    }

    private async Task CacheResponseAsync(string name, string type, byte[] response, string? upstreamServer, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DnsContext>();
        var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

        await _cacheLock.WaitAsync();
        try
        {
            // Remove existing cache entry
            var existing = await context.CacheRecords
                .FirstOrDefaultAsync(c => c.Name == name && c.Type == type);

            if (existing != null)
            {
                context.CacheRecords.Remove(existing);
            }

            // Get cache timeout from configuration
            var cacheTimeoutMinutes = 60; // Default value
            try
            {
                cacheTimeoutMinutes = await configService.GetIntValueAsync("CacheTimeoutMinutes", 60);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not get cache timeout from configuration, using default: {Message}", ex.Message);
            }

            // Add new cache entry with configurable timeout
            var cacheRecord = new CacheRecord
            {
                Name = name,
                Type = type,
                Response = Convert.ToBase64String(response),
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(cacheTimeoutMinutes),
                UpstreamServer = upstreamServer
            };

            context.CacheRecords.Add(cacheRecord);
            await context.SaveChangesAsync(stoppingToken);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task CleanupExpiredCacheAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DnsContext>();

                var expiredRecords = await context.CacheRecords
                    .Where(c => c.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expiredRecords.Any())
                {
                    context.CacheRecords.RemoveRange(expiredRecords);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Removed {Count} expired cache records", expiredRecords.Count);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired cache");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    // Proper DNS message parsing
    private DnsMessage? ParseDnsMessage(byte[] data)
    {
        if (data.Length < 12) return null;

        try
        {
            // Extract transaction ID (first 2 bytes)
            var transactionId = (data[0] << 8) | data[1];

            // Extract flags (bytes 2-3)
            var flags = (data[2] << 8) | data[3];
            var isQuery = (flags & 0x8000) == 0;

            if (!isQuery) return null; // Only handle queries

            // Extract question count (bytes 4-5)
            var questionCount = (data[4] << 8) | data[5];

            if (questionCount != 1) return null; // Only handle single question

            // Parse the question section
            var name = ExtractName(data, 12);
            if (string.IsNullOrEmpty(name)) return null;

            // Find the end of the name
            var pos = 12;
            while (pos < data.Length && data[pos] != 0) pos++;
            pos++; // Skip the null terminator

            if (pos + 4 > data.Length) return null;

            // Extract type and class
            var type = (data[pos] << 8) | data[pos + 1];
            var dnsClass = (data[pos + 2] << 8) | data[pos + 3];

            if (dnsClass != 1) return null; // Only handle IN class

            return new DnsMessage
            {
                TransactionId = (ushort)transactionId,
                Name = name,
                Type = GetDnsTypeString(type)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing DNS message");
            return null;
        }
    }

    private string ExtractName(byte[] data, int offset)
    {
        try
        {
            var name = new StringBuilder();
            var pos = offset;

            while (pos < data.Length && data[pos] != 0)
            {
                var length = data[pos++];
                if (pos + length > data.Length) break;

                if (name.Length > 0) name.Append('.');
                name.Append(Encoding.ASCII.GetString(data, pos, length));
                pos += length;
            }

            return name.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private string GetDnsTypeString(int type)
    {
        return type switch
        {
            1 => "A",
            2 => "NS",
            5 => "CNAME",
            6 => "SOA",
            15 => "MX",
            16 => "TXT",
            28 => "AAAA",
            41 => "OPT",
            43 => "DS",
            46 => "RRSIG",
            47 => "NSEC",
            48 => "DNSKEY",
            _ => type.ToString()
        };
    }

    private byte[] CreateDnsRequest(DnsMessage dnsMessage)
    {
        var request = new List<byte>();

        // Header
        request.AddRange(new byte[] { 0x00, 0x01 }); // ID
        request.AddRange(new byte[] { 0x01, 0x00 }); // Flags
        request.AddRange(new byte[] { 0x00, 0x01 }); // Questions
        request.AddRange(new byte[] { 0x00, 0x00 }); // Answers
        request.AddRange(new byte[] { 0x00, 0x00 }); // Authority
        request.AddRange(new byte[] { 0x00, 0x00 }); // Additional

        // Question
        var nameParts = dnsMessage.Name.Split('.');
        foreach (var part in nameParts)
        {
            request.Add((byte)part.Length);
            request.AddRange(Encoding.ASCII.GetBytes(part));
        }
        request.Add(0x00); // End of name

        // Type and Class
        var type = GetDnsTypeInt(dnsMessage.Type);
        request.Add((byte)(type >> 8));
        request.Add((byte)(type & 0xFF));
        request.AddRange(new byte[] { 0x00, 0x01 }); // Class IN

        return request.ToArray();
    }

    private int GetDnsTypeInt(string type) => DnsRecordTypeWire.ToWireType(type);

    private byte[] CreateResponseFromCache(byte[] request, byte[] cachedResponse)
    {
        // For cached responses, we need to modify the cached response to use the original transaction ID
        var response = (byte[])cachedResponse.Clone();

        // Set the transaction ID from the original request
        response[0] = request[0];
        response[1] = request[1];

        return response;
    }

    private byte[] CreateAuthoritativeResponse(byte[] request, DnsMessage dnsMessage, DnsRecord record)
    {
        var response = new List<byte>();

        // Copy header from request
        response.AddRange(request.Take(12));

        // Set response flags (QR=1, AA=1, RA=1)
        response[2] = 0x84; // Response + Authoritative
        response[3] = 0x80; // Recursion available

        // Set answer count to 1
        response[6] = 0x00;
        response[7] = 0x01;

        // Copy question section
        var questionEnd = 12;
        while (questionEnd < request.Length && request[questionEnd] != 0) questionEnd++;
        questionEnd += 5; // Include null terminator and type/class
        response.AddRange(request.Skip(12).Take(questionEnd - 12));

        // Add answer section
        response.AddRange(CreateAnswerSection(dnsMessage.Name, dnsMessage.Type, record.Value, record.Ttl));

        return response.ToArray();
    }

    private byte[] CreateAnswerSection(string name, string type, string value, int ttl)
    {
        var answer = new List<byte>();

        // Name (compressed)
        answer.Add(0xC0); // Compression pointer
        answer.Add(0x0C); // Offset to name in question section

        // Type
        var typeInt = GetDnsTypeInt(type);
        answer.Add((byte)(typeInt >> 8));
        answer.Add((byte)(typeInt & 0xFF));

        // Class (IN)
        answer.Add(0x00);
        answer.Add(0x01);

        // TTL
        answer.Add((byte)(ttl >> 24));
        answer.Add((byte)(ttl >> 16));
        answer.Add((byte)(ttl >> 8));
        answer.Add((byte)(ttl & 0xFF));

        // Data length and value
        if (type.ToUpper() == "A")
        {
            var ipParts = value.Split('.');
            answer.Add(0x00);
            answer.Add(0x04); // Length for IPv4
            foreach (var part in ipParts)
            {
                answer.Add(byte.Parse(part));
            }
        }
        else
        {
            // For other types, add placeholder
            answer.Add(0x00);
            answer.Add(0x04);
            answer.AddRange(new byte[] { 192, 168, 1, 1 });
        }

        return answer.ToArray();
    }

    private byte[] CreateNxDomainResponse(byte[] request)
    {
        var response = (byte[])request.Clone();
        response[2] |= 0x80; // Response flag
        response[3] = 0x03; // NXDOMAIN
        return response;
    }

    private static byte[] CreateNoDataResponse(byte[] request)
    {
        var response = (byte[])request.Clone();
        response[2] = (byte)(0x84 | (request[2] & 0x01));
        response[3] = 0x80;
        return response;
    }

    private byte[] CreateErrorResponse(byte[] request)
    {
        var response = (byte[])request.Clone();
        response[2] |= 0x80; // Response flag
        response[3] = 0x02; // Server failure
        return response;
    }

    public override void Dispose()
    {
        _udpClient?.Dispose();
        _tcpListener?.Stop();
        _cacheLock?.Dispose();
        base.Dispose();
    }
}

public class DnsMessage
{
    public ushort TransactionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
