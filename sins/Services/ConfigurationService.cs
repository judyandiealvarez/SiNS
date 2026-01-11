using sins.Data;
using sins.Models;
using Dapper;

namespace sins.Services;

public interface IConfigurationService
{
    Task<string?> GetValueAsync(string key, string? defaultValue = null);
    Task SetValueAsync(string key, string value, string updatedBy);
    Task<int> GetIntValueAsync(string key, int defaultValue = 0);
    Task<string[]> GetStringArrayValueAsync(string key, string[] defaultValue);
    Task<bool> GetBoolValueAsync(string key, bool defaultValue = false);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConfigurationService(IDatabaseService databaseService, ILogger<ConfigurationService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(string key, string? defaultValue = null)
    {
        await _lock.WaitAsync();
        try
        {
            using var connection = _databaseService.GetConnection();
            var config = await connection.QueryFirstOrDefaultAsync<ServerConfig>(@"
                SELECT * FROM ""ServerConfigs""
                WHERE ""Key"" = @Key
                LIMIT 1
            ", new { Key = key });
            return config?.Value ?? defaultValue;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetValueAsync(string key, string value, string updatedBy)
    {
        await _lock.WaitAsync();
        try
        {
            using var connection = _databaseService.GetConnection();
            var exists = await connection.QueryFirstOrDefaultAsync<string>(@"
                SELECT ""Key"" FROM ""ServerConfigs""
                WHERE ""Key"" = @Key
                LIMIT 1
            ", new { Key = key });

            var updatedAt = DateTime.UtcNow;

            if (exists == null)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO ""ServerConfigs"" (""Key"", ""Value"", ""UpdatedAt"", ""UpdatedBy"")
                    VALUES (@Key, @Value, @UpdatedAt, @UpdatedBy)
                ", new { Key = key, Value = value, UpdatedAt = updatedAt, UpdatedBy = updatedBy });
            }
            else
            {
                await connection.ExecuteAsync(@"
                    UPDATE ""ServerConfigs""
                    SET ""Value"" = @Value, ""UpdatedAt"" = @UpdatedAt, ""UpdatedBy"" = @UpdatedBy
                    WHERE ""Key"" = @Key
                ", new { Key = key, Value = value, UpdatedAt = updatedAt, UpdatedBy = updatedBy });
            }

            _logger.LogInformation("Configuration updated: {Key} = {Value} by {UpdatedBy}", key, value, updatedBy);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue = 0)
    {
        var value = await GetValueAsync(key, defaultValue.ToString());
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<string[]> GetStringArrayValueAsync(string key, string[] defaultValue)
    {
        var value = await GetValueAsync(key);
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s))
                   .ToArray();
    }

    public async Task<bool> GetBoolValueAsync(string key, bool defaultValue = false)
    {
        var value = await GetValueAsync(key, defaultValue.ToString());
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
