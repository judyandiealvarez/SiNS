using System.CommandLine;
using Microsoft.Extensions.Logging;
using sins.cli.Models;
using sins.cli.Services;

namespace sins.cli.Commands;

public class SystemCommands
{
    private readonly ApiClient _apiClient;
    private readonly OutputService _outputService;
    private readonly ILogger<SystemCommands> _logger;

    public SystemCommands(ApiClient apiClient, OutputService outputService, ILogger<SystemCommands> logger)
    {
        _apiClient = apiClient;
        _outputService = outputService;
        _logger = logger;
    }

    public Command CreateSystemCommand()
    {
        var systemCommand = new Command("system", "System management")
        {
            CreateHealthCommand(),
            CreateStatsCommand(),
            CreateConfigCommand(),
            CreateVersionCommand()
        };

        return systemCommand;
    }

    private Command CreateHealthCommand()
    {
        var command = new Command("health", "Check system health");

        command.SetHandler(async () =>
        {
            try
            {
                var health = await _apiClient.GetHealthAsync();
                _outputService.DisplayHealthCheck(health);
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

    private Command CreateStatsCommand()
    {
        var command = new Command("stats", "Show system statistics");

        command.SetHandler(async () =>
        {
            try
            {
                var stats = await _apiClient.GetStatsAsync();
                _outputService.DisplayServerStats(stats);
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

    private Command CreateConfigCommand()
    {
        var configCommand = new Command("config", "Manage server configuration")
        {
            CreateGetConfigCommand(),
            CreateUpdateConfigCommand()
        };

        return configCommand;
    }

    private Command CreateGetConfigCommand()
    {
        var command = new Command("get", "Get current configuration");

        command.SetHandler(async () =>
        {
            try
            {
                var config = await _apiClient.GetConfigAsync();
                _outputService.DisplayServerConfig(config);
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

    private Command CreateUpdateConfigCommand()
    {
        var cacheTimeoutOption = new Option<int?>("--cache-timeout", "Cache timeout in minutes");
        var udpPortOption = new Option<int?>("--udp-port", "UDP port");
        var tcpPortOption = new Option<int?>("--tcp-port", "TCP port");
        var upstreamServersOption = new Option<string[]?>("--upstream-servers", "Upstream DNS servers (comma-separated)");

        var command = new Command("update", "Update server configuration")
        {
            cacheTimeoutOption,
            udpPortOption,
            tcpPortOption,
            upstreamServersOption
        };

        command.SetHandler(async (int? cacheTimeout, int? udpPort, int? tcpPort, string[]? upstreamServers) =>
        {
            try
            {
                var request = new ConfigUpdateRequest
                {
                    CacheTimeoutMinutes = cacheTimeout,
                    UdpPort = udpPort,
                    TcpPort = tcpPort,
                    UpstreamServers = upstreamServers
                };

                await _apiClient.UpdateConfigAsync(request);
                _outputService.WriteSuccess("Configuration updated successfully");
                
                // Show updated config
                var config = await _apiClient.GetConfigAsync();
                _outputService.DisplayServerConfig(config);
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, cacheTimeoutOption, udpPortOption, tcpPortOption, upstreamServersOption);

        return command;
    }

    private Command CreateVersionCommand()
    {
        var command = new Command("version", "Show server version");

        command.SetHandler(async () =>
        {
            try
            {
                var version = await _apiClient.GetVersionAsync();
                _outputService.DisplayVersion(version);
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
