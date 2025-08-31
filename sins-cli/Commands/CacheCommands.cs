using System.CommandLine;
using Microsoft.Extensions.Logging;
using sins.cli.Services;

namespace sins.cli.Commands;

public class CacheCommands
{
    private readonly ApiClient _apiClient;
    private readonly OutputService _outputService;
    private readonly ILogger<CacheCommands> _logger;

    public CacheCommands(ApiClient apiClient, OutputService outputService, ILogger<CacheCommands> logger)
    {
        _apiClient = apiClient;
        _outputService = outputService;
        _logger = logger;
    }

    public Command CreateCacheCommand()
    {
        var cacheCommand = new Command("cache", "Manage DNS cache")
        {
            CreateListCommand(),
            CreateClearAllCommand(),
            CreateClearExpiredCommand()
        };

        return cacheCommand;
    }

    private Command CreateListCommand()
    {
        var domainOption = new Option<string?>("--domain", "Filter by domain name");
        var expiredOption = new Option<bool?>("--expired", "Show expired records only");

        var command = new Command("list", "List cache records")
        {
            domainOption,
            expiredOption
        };

        command.SetHandler(async (string? domain, bool? expired) =>
        {
            try
            {
                var records = await _apiClient.GetCacheRecordsAsync();

                // Apply filters if provided
                if (!string.IsNullOrEmpty(domain))
                {
                    records = records.Where(r => r.Domain.Contains(domain, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (expired.HasValue)
                {
                    var now = DateTime.UtcNow;
                    if (expired.Value)
                    {
                        records = records.Where(r => r.ExpiresAt <= now).ToList();
                    }
                    else
                    {
                        records = records.Where(r => r.ExpiresAt > now).ToList();
                    }
                }

                _outputService.DisplayCacheRecords(records);
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, domainOption, expiredOption);

        return command;
    }

    private Command CreateClearAllCommand()
    {
        var command = new Command("clear-all", "Clear all cache records");

        command.SetHandler(async () =>
        {
            try
            {
                await _apiClient.ClearAllCacheAsync();
                _outputService.WriteSuccess("All cache records cleared successfully");
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        });

        return command;
    }

    private Command CreateClearExpiredCommand()
    {
        var command = new Command("clear-expired", "Clear expired cache records");

        command.SetHandler(async () =>
        {
            try
            {
                await _apiClient.ClearExpiredCacheAsync();
                _outputService.WriteSuccess("Expired cache records cleared successfully");
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        });

        return command;
    }
}
