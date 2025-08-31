using System.CommandLine;
using sins.cli.Models;

namespace sins.cli.Services;

public class OutputService
{
    public OutputService()
    {
    }

    public void WriteSuccess(string message)
    {
        System.Console.WriteLine($"✅ {message}");
    }

    public void WriteError(string message)
    {
        System.Console.WriteLine($"❌ {message}");
    }

    public void WriteWarning(string message)
    {
        System.Console.WriteLine($"⚠️  {message}");
    }

    public void WriteInfo(string message)
    {
        System.Console.WriteLine($"ℹ️  {message}");
    }

    public void DisplayDnsRecords(List<DnsRecord> records)
    {
        if (!records.Any())
        {
            System.Console.WriteLine("No DNS records found.");
            return;
        }

        System.Console.WriteLine("\nDNS Records:");
        System.Console.WriteLine(new string('-', 80));
        System.Console.WriteLine($"{"ID",-4} {"Name",-25} {"Type",-8} {"Value",-25} {"TTL",-6} {"Active",-6}");
        System.Console.WriteLine(new string('-', 80));

        foreach (var record in records)
        {
            System.Console.WriteLine($"{record.Id,-4} {record.Name,-25} {record.Type,-8} {record.Value,-25} {record.Ttl,-6} {(record.IsActive ? "Yes" : "No"),-6}");
        }
        System.Console.WriteLine();
    }

    public void DisplayDnsRecord(DnsRecord record)
    {
        System.Console.WriteLine("\nDNS Record Details:");
        System.Console.WriteLine(new string('-', 50));
        System.Console.WriteLine($"ID: {record.Id}");
        System.Console.WriteLine($"Name: {record.Name}");
        System.Console.WriteLine($"Type: {record.Type}");
        System.Console.WriteLine($"Value: {record.Value}");
        System.Console.WriteLine($"TTL: {record.Ttl}");
        System.Console.WriteLine($"Active: {(record.IsActive ? "Yes" : "No")}");
        System.Console.WriteLine($"Created: {record.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine($"Updated: {record.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine();
    }

    public void DisplayCacheRecords(List<CacheRecord> records)
    {
        if (!records.Any())
        {
            System.Console.WriteLine("No cache records found.");
            return;
        }

        System.Console.WriteLine("\nCache Records:");
        System.Console.WriteLine(new string('-', 100));
        System.Console.WriteLine($"{"ID",-4} {"Domain",-25} {"Type",-8} {"IPs",-30} {"Expires",-20} {"Server",-15}");
        System.Console.WriteLine(new string('-', 100));

        foreach (var record in records)
        {
            var ips = string.Join(", ", record.ResolvedIPs);
            if (ips.Length > 28) ips = ips[..25] + "...";

            System.Console.WriteLine($"{record.Id,-4} {record.Domain,-25} {record.Type,-8} {ips,-30} {record.ExpiresAt:yyyy-MM-dd HH:mm:ss,-20} {record.UpstreamServer,-15}");
        }
        System.Console.WriteLine();
    }

    public void DisplayUsers(List<UserInfo> users)
    {
        if (!users.Any())
        {
            System.Console.WriteLine("No users found.");
            return;
        }

        System.Console.WriteLine("\nUsers:");
        System.Console.WriteLine(new string('-', 70));
        System.Console.WriteLine($"{"ID",-4} {"Username",-15} {"Email",-25} {"Role",-10} {"Created",-15}");
        System.Console.WriteLine(new string('-', 70));

        foreach (var user in users)
        {
            System.Console.WriteLine($"{user.Id,-4} {user.Username,-15} {user.Email,-25} {user.Role,-10} {user.CreatedAt:yyyy-MM-dd,-15}");
        }
        System.Console.WriteLine();
    }

    public void DisplayServerConfig(ServerConfig config)
    {
        System.Console.WriteLine("\nServer Configuration:");
        System.Console.WriteLine(new string('-', 50));
        System.Console.WriteLine($"Cache Timeout: {config.CacheTimeoutMinutes} minutes");
        System.Console.WriteLine($"UDP Port: {config.UdpPort}");
        System.Console.WriteLine($"TCP Port: {config.TcpPort}");
        System.Console.WriteLine($"Upstream Servers: {string.Join(", ", config.UpstreamServers)}");
        System.Console.WriteLine();
    }

    public void DisplayServerStats(ServerStats stats)
    {
        System.Console.WriteLine("\nServer Statistics:");
        System.Console.WriteLine(new string('-', 50));
        System.Console.WriteLine($"Total DNS Records: {stats.TotalRecords}");
        System.Console.WriteLine($"Active Cache Records: {stats.TotalCacheRecords}");
        System.Console.WriteLine($"Expired Cache Records: {stats.ExpiredCacheRecords}");
        System.Console.WriteLine($"Cache Hit Rate: {stats.CacheHitRate:P2}");
        System.Console.WriteLine();
    }

    public void DisplayHealthCheck(HealthCheck health)
    {
        System.Console.WriteLine("\nHealth Check:");
        System.Console.WriteLine(new string('-', 30));
        System.Console.WriteLine($"Status: {health.Status}");
        System.Console.WriteLine($"Timestamp: {health.Timestamp:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine();
    }

    public void DisplayVersion(VersionInfo version)
    {
        System.Console.WriteLine($"\nSiNS DNS Server Version: {version.Version}");
        System.Console.WriteLine();
    }

    public void DisplayApiError(ApiException ex)
    {
        WriteError($"API Error: {ex.Message}");
    }

    public void DisplayException(Exception ex)
    {
        WriteError($"Error: {ex.Message}");
        if (ex.InnerException != null)
        {
            WriteError($"Inner Error: {ex.InnerException.Message}");
        }
    }
}
