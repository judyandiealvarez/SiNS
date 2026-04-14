using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sins.Data;
using sins.Models;
using sins.Services;
using Microsoft.Extensions.Logging;

namespace sins.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DnsController : ControllerBase
{
    private readonly DnsContext _context;
    private readonly IConfigurationService _configService;
    private readonly IDnssecCatalog _dnssecCatalog;
    private readonly ILogger<DnsController> _logger;

    public DnsController(DnsContext context, IConfigurationService configService, IDnssecCatalog dnssecCatalog,
        ILogger<DnsController> logger)
    {
        _context = context;
        _configService = configService;
        _dnssecCatalog = dnssecCatalog;
        _logger = logger;
        _logger.LogInformation("[DEBUG] DnsController constructor called");
    }

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords()
    {
        var records = await _context.DnsRecords
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Type)
            .ToListAsync();

        return Ok(records);
    }



    [HttpGet("records/{id}")]
    public async Task<IActionResult> GetRecord(int id)
    {
        var record = await _context.DnsRecords.FindAsync(id);

        if (record == null)
        {
            return NotFound();
        }

        return Ok(record);
    }

    [HttpPost("records")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRecord([FromBody] CreateDnsRecordRequest request)
    {
        _logger.LogInformation($"[DEBUG] CreateRecord called with: Name='{request?.Name}', Type='{request?.Type}', Value='{request?.Value}', Ttl={request?.Ttl}");

        if (request == null)
        {
            _logger.LogWarning("[DEBUG] Request body is null");
            return BadRequest(new { message = "Request body is required" });
        }

        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Value))
        {
            _logger.LogWarning($"[DEBUG] Validation failed: Name='{request.Name}', Type='{request.Type}', Value='{request.Value}'");
            return BadRequest(new { message = "Name, type, and value are required" });
        }

        _logger.LogInformation($"[DEBUG] Creating record: {request.Name} ({request.Type}) = {request.Value}");

        var record = new DnsRecord
        {
            Name = request.Name,
            Type = request.Type.ToUpper(),
            Value = request.Value,
            Ttl = request.Ttl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation($"[DEBUG] Adding record to context");
            _context.DnsRecords.Add(record);
            _logger.LogInformation($"[DEBUG] Saving changes to database");
            await _context.SaveChangesAsync();
            await InvalidateDnssecZonesAsync();
            _logger.LogInformation($"[DEBUG] Record created successfully with ID: {record.Id}");
            return CreatedAtAction(nameof(GetRecord), new { id = record.Id }, record);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError($"[DEBUG] DbUpdateException: {ex.Message}");
            _logger.LogError($"[DEBUG] InnerException: {ex.InnerException?.Message}");

            if (ex.InnerException?.Message?.Contains("duplicate key") == true ||
                ex.InnerException?.Message?.Contains("IX_DnsRecords_Name_Type") == true)
            {
                _logger.LogWarning($"[DEBUG] Duplicate key detected for {request.Name} ({request.Type})");
                return BadRequest(new { message = $"A DNS record with name '{request.Name}' and type '{request.Type}' already exists." });
            }
            _logger.LogError($"[DEBUG] Unknown database error: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while creating the DNS record." });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DEBUG] General exception: {ex.Message}");
            _logger.LogError($"[DEBUG] Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = "An error occurred while creating the DNS record." });
        }
    }

    [HttpPut("records/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] UpdateDnsRecordRequest request)
    {
        var record = await _context.DnsRecords.FindAsync(id);

        if (record == null)
        {
            return NotFound();
        }

        record.Name = request.Name;
        record.Type = request.Type.ToUpper();
        record.Value = request.Value;
        record.Ttl = request.Ttl;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await InvalidateDnssecZonesAsync();

        return Ok(record);
    }

    [HttpDelete("records/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRecord(int id)
    {
        var record = await _context.DnsRecords.FindAsync(id);

        if (record == null)
        {
            return NotFound();
        }

        // Real delete - remove the record from database
        _context.DnsRecords.Remove(record);
        await _context.SaveChangesAsync();
        await InvalidateDnssecZonesAsync();

        return NoContent();
    }

    private async Task InvalidateDnssecZonesAsync()
    {
        var apexes = await _context.DnssecZones.Select(z => z.Apex).ToListAsync();
        foreach (var a in apexes)
            _dnssecCatalog.InvalidateZone(a);
    }

    [HttpGet("cache")]
    public async Task<IActionResult> GetCache()
    {
        var cacheRecords = await _context.CacheRecords
            .Where(c => c.ExpiresAt > DateTime.UtcNow)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Type)
            .ToListAsync();

        return Ok(cacheRecords);
    }

    [HttpGet("cache/details")]
    public async Task<IActionResult> GetCacheDetails()
    {
        var cacheRecords = await _context.CacheRecords
            .Where(c => c.ExpiresAt > DateTime.UtcNow)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Type)
            .ToListAsync();

        var detailedRecords = new List<object>();

        foreach (var record in cacheRecords)
        {
            try
            {
                var responseBytes = Convert.FromBase64String(record.Response);
                var resolvedIps = ExtractIpsFromDnsResponse(responseBytes, record.Type);

                detailedRecords.Add(new
                {
                    record.Id,
                    record.Name,
                    record.Type,
                    record.CachedAt,
                    record.ExpiresAt,
                    record.UpstreamServer,
                    ResolvedIPs = resolvedIps,
                    ResponseSize = responseBytes.Length
                });
            }
            catch
            {
                // If we can't parse the response, return basic info
                detailedRecords.Add(new
                {
                    record.Id,
                    record.Name,
                    record.Type,
                    record.CachedAt,
                    record.ExpiresAt,
                    record.UpstreamServer,
                    ResolvedIPs = new string[0],
                    ResponseSize = 0
                });
            }
        }

        return Ok(detailedRecords);
    }

    private List<string> ExtractIpsFromDnsResponse(byte[] response, string type)
    {
        var ips = new List<string>();

        try
        {
            if (response.Length < 12) return ips;

            // Parse DNS header
            var answerCount = (response[6] << 8) | response[7];

            if (answerCount == 0) return ips;

            // For A records, look for the last 4 bytes which should be the IP address
            if (type.ToUpper() == "A" && response.Length >= 4)
            {
                var last4Bytes = response.Skip(response.Length - 4).Take(4).ToArray();
                var ip = $"{last4Bytes[0]}.{last4Bytes[1]}.{last4Bytes[2]}.{last4Bytes[3]}";
                ips.Add(ip);
            }
            // For AAAA records, look for the last 16 bytes
            else if (type.ToUpper() == "AAAA" && response.Length >= 16)
            {
                var last16Bytes = response.Skip(response.Length - 16).Take(16).ToArray();
                var ipv6 = BitConverter.ToString(last16Bytes).Replace("-", ":");
                ips.Add(ipv6);
            }
        }
        catch (Exception ex)
        {
            // Log the error for debugging
            Console.WriteLine($"Error parsing DNS response: {ex.Message}");
        }

        return ips;
    }

    private int SkipDnsName(byte[] response, int pos)
    {
        try
        {
            while (pos < response.Length)
            {
                if ((response[pos] & 0xC0) == 0xC0)
                {
                    // Compressed name - skip 2 bytes
                    return pos + 2;
                }
                else if (response[pos] == 0)
                {
                    // End of name
                    return pos + 1;
                }
                else
                {
                    // Regular name - skip length byte and the name bytes
                    var length = response[pos];
                    pos += length + 1;
                }
            }
            return -1; // Error
        }
        catch
        {
            return -1; // Error
        }
    }

    [HttpDelete("cache")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearAllCache()
    {
        var allRecords = await _context.CacheRecords.ToListAsync();

        _context.CacheRecords.RemoveRange(allRecords);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Cleared all {allRecords.Count} cache records" });
    }

    [HttpDelete("cache/expired")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearExpiredCache()
    {
        var expiredRecords = await _context.CacheRecords
            .Where(c => c.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        _context.CacheRecords.RemoveRange(expiredRecords);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Cleared {expiredRecords.Count} expired cache records" });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalRecords = await _context.DnsRecords.CountAsync();
        var totalCacheRecords = await _context.CacheRecords.CountAsync(c => c.ExpiresAt > DateTime.UtcNow);
        var expiredCacheRecords = await _context.CacheRecords.CountAsync(c => c.ExpiresAt <= DateTime.UtcNow);

        return Ok(new
        {
            totalRecords,
            totalCacheRecords,
            expiredCacheRecords,
            cacheHitRate = totalCacheRecords > 0 ? (double)totalCacheRecords / (totalCacheRecords + expiredCacheRecords) : 0
        });
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("Health check failed: Database connection unavailable");
                return StatusCode(503, new { 
                    status = "unhealthy", 
                    reason = "database_connection_failed",
                    timestamp = DateTime.UtcNow 
                });
            }

            // Verify required tables exist by attempting to query them
            // This will throw if tables don't exist
            var dnsRecordsCount = await _context.DnsRecords.CountAsync();
            var cacheRecordsCount = await _context.CacheRecords.CountAsync();
            var usersCount = await _context.Users.CountAsync();
            var serverConfigsCount = await _context.ServerConfigs.CountAsync();

            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                database = new {
                    connected = true,
                    tables = new {
                        DnsRecords = dnsRecordsCount,
                        CacheRecords = cacheRecordsCount,
                        Users = usersCount,
                        ServerConfigs = serverConfigsCount
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed: {Message}", ex.Message);
            return StatusCode(503, new { 
                status = "unhealthy", 
                reason = "database_error",
                error = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var cacheTimeoutMinutes = await _configService.GetIntValueAsync("CacheTimeoutMinutes", 60);
        var upstreamServers = await _configService.GetStringArrayValueAsync("UpstreamServers", new[] { "8.8.8.8", "1.1.1.1" });
        var udpPort = await _configService.GetIntValueAsync("UdpPort", 53);
        var tcpPort = await _configService.GetIntValueAsync("TcpPort", 53);
        var haproxy = await _configService.GetValueAsync("Haproxy", null);

        return Ok(new
        {
            CacheTimeoutMinutes = cacheTimeoutMinutes,
            UpstreamServers = upstreamServers,
            UdpPort = udpPort,
            TcpPort = tcpPort,
            Haproxy = haproxy
        });
    }

    [HttpPost("config")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateConfig([FromBody] ConfigUpdateRequest request)
    {
        try
        {
            var username = User.Identity?.Name ?? "Unknown";

            if (request.CacheTimeoutMinutes.HasValue)
            {
                await _configService.SetValueAsync("CacheTimeoutMinutes", request.CacheTimeoutMinutes.Value.ToString(), username);
            }

            if (request.UdpPort.HasValue)
            {
                await _configService.SetValueAsync("UdpPort", request.UdpPort.Value.ToString(), username);
            }

            if (request.TcpPort.HasValue)
            {
                await _configService.SetValueAsync("TcpPort", request.TcpPort.Value.ToString(), username);
            }

            if (request.UpstreamServers != null && request.UpstreamServers.Length > 0)
            {
                var upstreamServersString = string.Join(",", request.UpstreamServers);
                await _configService.SetValueAsync("UpstreamServers", upstreamServersString, username);
            }

            if (request.Haproxy != null)
            {
                await _configService.SetValueAsync("Haproxy", request.Haproxy, username);
            }

            return Ok(new { message = "Configuration updated successfully. Changes will take effect immediately." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error updating configuration: {ex.Message}" });
        }
    }

    [HttpGet("domain-upstreams")]
    public async Task<IActionResult> GetDomainUpstreamMappings()
    {
        var list = await _context.DomainUpstreamMappings
            .OrderBy(m => m.Domain)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("domain-upstreams")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDomainUpstreamMapping([FromBody] DomainUpstreamMappingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Domain) || string.IsNullOrWhiteSpace(request.UpstreamServer))
            return BadRequest(new { message = "Domain and UpstreamServer are required" });

        var domain = request.Domain.Trim().TrimEnd('.').ToLowerInvariant();
        var upstream = request.UpstreamServer.Trim();

        var existing = await _context.DomainUpstreamMappings.FirstOrDefaultAsync(m => m.Domain == domain);
        if (existing != null)
            return BadRequest(new { message = $"A mapping for domain '{domain}' already exists. Use PUT to update." });

        var mapping = new DomainUpstreamMapping
        {
            Domain = domain,
            UpstreamServer = upstream,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DomainUpstreamMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return Created($"/api/dns/domain-upstreams/{mapping.Id}", mapping);
    }

    [HttpPut("domain-upstreams/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDomainUpstreamMapping(int id, [FromBody] DomainUpstreamMappingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Domain) || string.IsNullOrWhiteSpace(request.UpstreamServer))
            return BadRequest(new { message = "Domain and UpstreamServer are required" });

        var mapping = await _context.DomainUpstreamMappings.FindAsync(id);
        if (mapping == null)
            return NotFound();

        var domain = request.Domain.Trim().TrimEnd('.').ToLowerInvariant();
        var upstream = request.UpstreamServer.Trim();

        var existingOther = await _context.DomainUpstreamMappings.FirstOrDefaultAsync(m => m.Domain == domain && m.Id != id);
        if (existingOther != null)
            return BadRequest(new { message = $"A mapping for domain '{domain}' already exists (id {existingOther.Id})." });

        mapping.Domain = domain;
        mapping.UpstreamServer = upstream;
        mapping.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(mapping);
    }

    [HttpDelete("domain-upstreams/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDomainUpstreamMapping(int id)
    {
        var mapping = await _context.DomainUpstreamMappings.FindAsync(id);
        if (mapping == null)
            return NotFound();

        _context.DomainUpstreamMappings.Remove(mapping);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("version")]
    [AllowAnonymous]
    public IActionResult GetVersion()
    {
        var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0.0";
        var buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "0";

        return Ok(new { version = $"{version}.{buildNumber}" });
    }
}

public class CreateDnsRecordRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Ttl { get; set; } = 3600;
}

public class UpdateDnsRecordRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Ttl { get; set; } = 3600;
}

public class ConfigUpdateRequest
{
    public int? CacheTimeoutMinutes { get; set; }
    public string[]? UpstreamServers { get; set; }
    public int? UdpPort { get; set; }
    public int? TcpPort { get; set; }
    /// <summary>HAProxy IP for Kubernetes ingress-backed DNS. Set to empty string to clear.</summary>
    public string? Haproxy { get; set; }
}

public class DomainUpstreamMappingRequest
{
    public string Domain { get; set; } = string.Empty;
    public string UpstreamServer { get; set; } = string.Empty;
}
