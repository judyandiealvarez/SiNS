using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sins.Data;
using sins.Models;
using sins.Services;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Data;
using Npgsql;

namespace sins.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DnsController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IConfigurationService _configService;
    private readonly ILogger<DnsController> _logger;

    public DnsController(IDatabaseService databaseService, IConfigurationService configService, ILogger<DnsController> logger)
    {
        _databaseService = databaseService;
        _configService = configService;
        _logger = logger;
        _logger.LogInformation("[DEBUG] DnsController constructor called");
    }

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords()
    {
        using var connection = _databaseService.GetConnection();
        var records = await connection.QueryAsync<DnsRecord>(@"
            SELECT * FROM ""DnsRecords""
            ORDER BY ""Name"", ""Type""
        ");

        return Ok(records);
    }



    [HttpGet("records/{id}")]
    public async Task<IActionResult> GetRecord(int id)
    {
        using var connection = _databaseService.GetConnection();
        var record = await connection.QueryFirstOrDefaultAsync<DnsRecord>(@"
            SELECT * FROM ""DnsRecords""
            WHERE ""Id"" = @Id
        ", new { Id = id });

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
            _logger.LogInformation($"[DEBUG] Adding record to database");
            using var connection = _databaseService.GetConnection();
            var id = await connection.QuerySingleAsync<int>(@"
                INSERT INTO ""DnsRecords"" (""Name"", ""Type"", ""Value"", ""Ttl"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@Name, @Type, @Value, @Ttl, @CreatedAt, @UpdatedAt)
                RETURNING ""Id""
            ", record);
            record.Id = id;
            _logger.LogInformation($"[DEBUG] Record created successfully with ID: {record.Id}");
            return CreatedAtAction(nameof(GetRecord), new { id = record.Id }, record);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") // Unique violation
        {
            _logger.LogError($"[DEBUG] PostgresException: {ex.Message}");
            if (ex.ConstraintName == "DnsRecords_Name_Type_key")
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
        using var connection = _databaseService.GetConnection();
        var existing = await connection.QueryFirstOrDefaultAsync<DnsRecord>(@"
            SELECT * FROM ""DnsRecords""
            WHERE ""Id"" = @Id
        ", new { Id = id });

        if (existing == null)
        {
            return NotFound();
        }

        var updatedAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"
            UPDATE ""DnsRecords""
            SET ""Name"" = @Name, ""Type"" = @Type, ""Value"" = @Value, ""Ttl"" = @Ttl, ""UpdatedAt"" = @UpdatedAt
            WHERE ""Id"" = @Id
        ", new
        {
            Id = id,
            Name = request.Name,
            Type = request.Type.ToUpper(),
            Value = request.Value,
            Ttl = request.Ttl,
            UpdatedAt = updatedAt
        });

        var record = await connection.QueryFirstOrDefaultAsync<DnsRecord>(@"
            SELECT * FROM ""DnsRecords""
            WHERE ""Id"" = @Id
        ", new { Id = id });

        return Ok(record);
    }

    [HttpDelete("records/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRecord(int id)
    {
        using var connection = _databaseService.GetConnection();
        var deleted = await connection.ExecuteAsync(@"
            DELETE FROM ""DnsRecords""
            WHERE ""Id"" = @Id
        ", new { Id = id });

        if (deleted == 0)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("cache")]
    public async Task<IActionResult> GetCache()
    {
        using var connection = _databaseService.GetConnection();
        var cacheRecords = await connection.QueryAsync<CacheRecord>(@"
            SELECT * FROM ""CacheRecords""
            WHERE ""ExpiresAt"" > @Now
            ORDER BY ""Name"", ""Type""
        ", new { Now = DateTime.UtcNow });

        return Ok(cacheRecords);
    }

    [HttpGet("cache/details")]
    public async Task<IActionResult> GetCacheDetails()
    {
        using var connection = _databaseService.GetConnection();
        var cacheRecords = await connection.QueryAsync<CacheRecord>(@"
            SELECT * FROM ""CacheRecords""
            WHERE ""ExpiresAt"" > @Now
            ORDER BY ""Name"", ""Type""
        ", new { Now = DateTime.UtcNow });

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
        using var connection = _databaseService.GetConnection();
        var count = await connection.ExecuteAsync(@"
            DELETE FROM ""CacheRecords""
        ");

        return Ok(new { message = $"Cleared all {count} cache records" });
    }

    [HttpDelete("cache/expired")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearExpiredCache()
    {
        using var connection = _databaseService.GetConnection();
        var count = await connection.ExecuteAsync(@"
            DELETE FROM ""CacheRecords""
            WHERE ""ExpiresAt"" <= @Now
        ", new { Now = DateTime.UtcNow });

        return Ok(new { message = $"Cleared {count} expired cache records" });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        using var connection = _databaseService.GetConnection();
        var now = DateTime.UtcNow;
        
        var totalRecords = await connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*) FROM ""DnsRecords""
        ");
        
        var totalCacheRecords = await connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*) FROM ""CacheRecords""
            WHERE ""ExpiresAt"" > @Now
        ", new { Now = now });
        
        var expiredCacheRecords = await connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*) FROM ""CacheRecords""
            WHERE ""ExpiresAt"" <= @Now
        ", new { Now = now });

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
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var cacheTimeoutMinutes = await _configService.GetIntValueAsync("CacheTimeoutMinutes", 60);
        var upstreamServers = await _configService.GetStringArrayValueAsync("UpstreamServers", new[] { "8.8.8.8", "1.1.1.1" });
        var udpPort = await _configService.GetIntValueAsync("UdpPort", 53);
        var tcpPort = await _configService.GetIntValueAsync("TcpPort", 53);

        return Ok(new
        {
            CacheTimeoutMinutes = cacheTimeoutMinutes,
            UpstreamServers = upstreamServers,
            UdpPort = udpPort,
            TcpPort = tcpPort
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

            return Ok(new { message = "Configuration updated successfully. Changes will take effect immediately." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error updating configuration: {ex.Message}" });
        }
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
}
// Trigger new deployment
