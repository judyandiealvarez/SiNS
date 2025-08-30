using System.Text.Json.Serialization;

namespace sins.cli.Models;

// Authentication Models
public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class RegisterRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "User";
}

// DNS Record Models
public class DnsRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateDnsRecordRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; } = 3600;
}

public class UpdateDnsRecordRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; } = 3600;
}

// Cache Models
public class CacheRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("resolvedIPs")]
    public string[] ResolvedIPs { get; set; } = Array.Empty<string>();

    [JsonPropertyName("upstreamServer")]
    public string UpstreamServer { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}

// Configuration Models
public class ServerConfig
{
    [JsonPropertyName("cacheTimeoutMinutes")]
    public int CacheTimeoutMinutes { get; set; }

    [JsonPropertyName("upstreamServers")]
    public string[] UpstreamServers { get; set; } = Array.Empty<string>();

    [JsonPropertyName("udpPort")]
    public int UdpPort { get; set; }

    [JsonPropertyName("tcpPort")]
    public int TcpPort { get; set; }
}

public class ConfigUpdateRequest
{
    [JsonPropertyName("cacheTimeoutMinutes")]
    public int? CacheTimeoutMinutes { get; set; }

    [JsonPropertyName("upstreamServers")]
    public string[]? UpstreamServers { get; set; }

    [JsonPropertyName("udpPort")]
    public int? UdpPort { get; set; }

    [JsonPropertyName("tcpPort")]
    public int? TcpPort { get; set; }
}

// Statistics Models
public class ServerStats
{
    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("totalCacheRecords")]
    public int TotalCacheRecords { get; set; }

    [JsonPropertyName("expiredCacheRecords")]
    public int ExpiredCacheRecords { get; set; }

    [JsonPropertyName("cacheHitRate")]
    public double CacheHitRate { get; set; }
}

// Health Check Models
public class HealthCheck
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class VersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

// Error Models
public class ApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
