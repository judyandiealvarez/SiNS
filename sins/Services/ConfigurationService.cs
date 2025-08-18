using Microsoft.EntityFrameworkCore;
using sins.Data;
using sins.Models;

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
    private readonly DnsContext _context;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConfigurationService(DnsContext context, ILogger<ConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(string key, string? defaultValue = null)
    {
        await _lock.WaitAsync();
        try
        {
            var config = await _context.ServerConfigs.FirstOrDefaultAsync(c => c.Key == key);
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
            var config = await _context.ServerConfigs.FirstOrDefaultAsync(c => c.Key == key);
            
            if (config == null)
            {
                config = new ServerConfig
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = updatedBy
                };
                _context.ServerConfigs.Add(config);
            }
            else
            {
                config.Value = value;
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedBy = updatedBy;
            }

            await _context.SaveChangesAsync();
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
