using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using sins.cli.Models;

namespace sins.cli.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public void SetBaseUrl(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    // Authentication Methods
    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Login failed: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize login response");
    }

    public async Task<List<UserInfo>> GetUsersAsync()
    {
        var response = await _httpClient.GetAsync("/api/auth/users");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to get users: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<List<UserInfo>>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize users response");
    }

    public async Task RegisterUserAsync(string username, string password, string email, string role = "User")
    {
        var request = new RegisterRequest
        {
            Username = username,
            Password = password,
            Email = email,
            Role = role
        };

        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Registration failed: {error?.Message ?? response.StatusCode.ToString()}");
        }
    }

    // DNS Record Methods
    public async Task<List<DnsRecord>> GetDnsRecordsAsync()
    {
        var response = await _httpClient.GetAsync("/api/dns/records");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to get DNS records: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<List<DnsRecord>>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize DNS records response");
    }

    public async Task<DnsRecord> GetDnsRecordAsync(int id)
    {
        var response = await _httpClient.GetAsync($"/api/dns/records/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to get DNS record: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<DnsRecord>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize DNS record response");
    }

    public async Task<DnsRecord> CreateDnsRecordAsync(string name, string type, string value, int ttl = 3600)
    {
        var request = new CreateDnsRecordRequest
        {
            Name = name,
            Type = type,
            Value = value,
            Ttl = ttl
        };

        var response = await _httpClient.PostAsJsonAsync("/api/dns/records", request, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to create DNS record: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<DnsRecord>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize create DNS record response");
    }

    public async Task<DnsRecord> UpdateDnsRecordAsync(int id, string name, string type, string value, int ttl = 3600)
    {
        var request = new UpdateDnsRecordRequest
        {
            Name = name,
            Type = type,
            Value = value,
            Ttl = ttl
        };

        var response = await _httpClient.PutAsJsonAsync($"/api/dns/records/{id}", request, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to update DNS record: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<DnsRecord>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize update DNS record response");
    }

    public async Task DeleteDnsRecordAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"/api/dns/records/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to delete DNS record: {error?.Message ?? response.StatusCode.ToString()}");
        }
    }

    // Cache Methods
    public async Task<List<CacheRecord>> GetCacheRecordsAsync()
    {
        var response = await _httpClient.GetAsync("/api/dns/cache");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to get cache records: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<List<CacheRecord>>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize cache records response");
    }

    public async Task ClearAllCacheAsync()
    {
        var response = await _httpClient.DeleteAsync("/api/dns/cache");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to clear cache: {error?.Message ?? response.StatusCode.ToString()}");
        }
    }

    public async Task ClearExpiredCacheAsync()
    {
        var response = await _httpClient.DeleteAsync("/api/dns/cache/expired");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to clear expired cache: {error?.Message ?? response.StatusCode.ToString()}");
        }
    }

    // Configuration Methods
    public async Task<ServerConfig> GetConfigAsync()
    {
        var response = await _httpClient.GetAsync("/api/dns/config");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to get config: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<ServerConfig>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize config response");
    }

    public async Task UpdateConfigAsync(ConfigUpdateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/dns/config", request, _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to update config: {error?.Message ?? response.StatusCode.ToString()}");
        }
    }

    // Statistics Methods
    public async Task<ServerStats> GetStatsAsync()
    {
        var response = await _httpClient.GetAsync("/api/dns/stats");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to get stats: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<ServerStats>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize stats response");
    }

    // Health Check Methods
    public async Task<HealthCheck> GetHealthAsync()
    {
        var response = await _httpClient.GetAsync("/api/dns/health");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Health check failed: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<HealthCheck>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize health check response");
    }

    public async Task<VersionInfo> GetVersionAsync()
    {
        var response = await _httpClient.GetAsync("/api/dns/version");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions);
            throw new ApiException($"Failed to get version: {error?.Message ?? response.StatusCode.ToString()}");
        }

        return await response.Content.ReadFromJsonAsync<VersionInfo>(_jsonOptions)
               ?? throw new ApiException("Failed to deserialize version response");
    }
}

public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
    public ApiException(string message, Exception innerException) : base(message, innerException) { }
}

