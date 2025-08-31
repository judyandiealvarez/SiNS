using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sins.cli.Commands;
using sins.cli.Services;
using System.CommandLine;

namespace sins.cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        // Configure services
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddHttpClient<ApiClient>();
        services.AddTransient<OutputService>();
        services.AddTransient<AuthCommands>();
        services.AddTransient<DnsCommands>();
        services.AddTransient<CacheCommands>();
        services.AddTransient<SystemCommands>();

        var serviceProvider = services.BuildServiceProvider();

        // Create root command
        var serverOption = new Option<string>("--server", () => "http://localhost", "Server URL");
        var tokenOption = new Option<string?>("--token", "Authentication token (or use SINS_TOKEN env var)");

        var rootCommand = new RootCommand("SiNS DNS Server CLI - Manage your DNS server via API")
        {
            serverOption,
            tokenOption
        };

        // Get services
        var apiClient = serviceProvider.GetRequiredService<ApiClient>();
        var outputService = serviceProvider.GetRequiredService<OutputService>();

        // Configure API client
        rootCommand.SetHandler((string server, string? token) =>
        {
            apiClient.SetBaseUrl(server);

            // Use provided token or environment variable
            var authToken = token ?? Environment.GetEnvironmentVariable("SINS_TOKEN");
            if (!string.IsNullOrEmpty(authToken))
            {
                apiClient.SetAuthToken(authToken);
            }
        }, serverOption, tokenOption);

        // Add subcommands
        var authCommands = serviceProvider.GetRequiredService<AuthCommands>();
        var dnsCommands = serviceProvider.GetRequiredService<DnsCommands>();
        var cacheCommands = serviceProvider.GetRequiredService<CacheCommands>();
        var systemCommands = serviceProvider.GetRequiredService<SystemCommands>();

        rootCommand.AddCommand(authCommands.CreateAuthCommand());
        rootCommand.AddCommand(dnsCommands.CreateDnsCommand());
        rootCommand.AddCommand(cacheCommands.CreateCacheCommand());
        rootCommand.AddCommand(systemCommands.CreateSystemCommand());

        // Add global options to all commands
        foreach (var command in rootCommand.Children.OfType<Command>())
        {
            foreach (var option in rootCommand.Options)
            {
                if (!command.Options.Any(o => o.Name == option.Name))
                {
                    command.AddOption(option);
                }
            }
        }

        return await rootCommand.InvokeAsync(args);
    }


}
