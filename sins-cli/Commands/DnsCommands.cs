using System.CommandLine;
using Microsoft.Extensions.Logging;
using sins.cli.Models;
using sins.cli.Services;

namespace sins.cli.Commands;

public class DnsCommands
{
    private readonly ApiClient _apiClient;
    private readonly OutputService _outputService;
    private readonly ILogger<DnsCommands> _logger;

    public DnsCommands(ApiClient apiClient, OutputService outputService, ILogger<DnsCommands> logger)
    {
        _apiClient = apiClient;
        _outputService = outputService;
        _logger = logger;
    }

    public Command CreateDnsCommand()
    {
        var dnsCommand = new Command("dns", "Manage DNS records")
        {
            CreateListCommand(),
            CreateGetCommand(),
            CreateAddCommand(),
            CreateUpdateCommand(),
            CreateDeleteCommand()
        };

        return dnsCommand;
    }

    private Command CreateListCommand()
    {
        var typeOption = new Option<string?>("--type", "Filter by record type (A, AAAA, CNAME, etc.)");
        var nameOption = new Option<string?>("--name", "Filter by domain name");
        
        var command = new Command("list", "List all DNS records")
        {
            typeOption,
            nameOption
        };

        command.SetHandler(async (string? type, string? name) =>
        {
            try
            {
                var records = await _apiClient.GetDnsRecordsAsync();
                
                // Apply filters if provided
                if (!string.IsNullOrEmpty(type))
                {
                    records = records.Where(r => r.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                if (!string.IsNullOrEmpty(name))
                {
                    records = records.Where(r => r.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                _outputService.DisplayDnsRecords(records);
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, typeOption, nameOption);

        return command;
    }

    private Command CreateGetCommand()
    {
        var idOption = new Option<int>("--id", "DNS record ID") { IsRequired = true };

        var command = new Command("get", "Get a specific DNS record")
        {
            idOption
        };

        command.SetHandler(async (int id) =>
        {
            try
            {
                var record = await _apiClient.GetDnsRecordAsync(id);
                _outputService.DisplayDnsRecord(record);
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, idOption);

        return command;
    }

    private Command CreateAddCommand()
    {
        var nameOption = new Option<string>("--name", "Domain name") { IsRequired = true };
        var typeOption = new Option<string>("--type", "Record type (A, AAAA, CNAME, MX, etc.)") { IsRequired = true };
        var valueOption = new Option<string>("--value", "Record value") { IsRequired = true };
        var ttlOption = new Option<int>("--ttl", () => 3600, "Time to live in seconds");

        var command = new Command("add", "Add a new DNS record")
        {
            nameOption,
            typeOption,
            valueOption,
            ttlOption
        };

        command.SetHandler(async (string name, string type, string value, int ttl) =>
        {
            try
            {
                var record = await _apiClient.CreateDnsRecordAsync(name, type, value, ttl);
                _outputService.WriteSuccess($"DNS record created successfully with ID: {record.Id}");
                _outputService.DisplayDnsRecord(record);
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, nameOption, typeOption, valueOption, ttlOption);

        return command;
    }

    private Command CreateUpdateCommand()
    {
        var idOption = new Option<int>("--id", "DNS record ID") { IsRequired = true };
        var nameOption = new Option<string>("--name", "Domain name") { IsRequired = true };
        var typeOption = new Option<string>("--type", "Record type (A, AAAA, CNAME, MX, etc.)") { IsRequired = true };
        var valueOption = new Option<string>("--value", "Record value") { IsRequired = true };
        var ttlOption = new Option<int>("--ttl", () => 3600, "Time to live in seconds");

        var command = new Command("update", "Update an existing DNS record")
        {
            idOption,
            nameOption,
            typeOption,
            valueOption,
            ttlOption
        };

        command.SetHandler(async (int id, string name, string type, string value, int ttl) =>
        {
            try
            {
                var record = await _apiClient.UpdateDnsRecordAsync(id, name, type, value, ttl);
                _outputService.WriteSuccess($"DNS record updated successfully");
                _outputService.DisplayDnsRecord(record);
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, idOption, nameOption, typeOption, valueOption, ttlOption);

        return command;
    }

    private Command CreateDeleteCommand()
    {
        var idOption = new Option<int>("--id", "DNS record ID") { IsRequired = true };

        var command = new Command("delete", "Delete a DNS record")
        {
            idOption
        };

        command.SetHandler(async (int id) =>
        {
            try
            {
                await _apiClient.DeleteDnsRecordAsync(id);
                _outputService.WriteSuccess($"DNS record with ID {id} deleted successfully");
            }
            catch (ApiException ex)
            {
                _outputService.DisplayApiError(ex);
            }
            catch (Exception ex)
            {
                _outputService.DisplayException(ex);
            }
        }, idOption);

        return command;
    }
}
