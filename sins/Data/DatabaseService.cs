using System.Data;
using Dapper;
using Npgsql;

namespace sins.Data;

public interface IDatabaseService
{
    IDbConnection GetConnection();
    Task EnsureTablesCreatedAsync();
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        _logger = logger;
    }

    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task EnsureTablesCreatedAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        // Create DnsRecords table
        var dnsRecordsTable = @"
            CREATE TABLE IF NOT EXISTS ""DnsRecords"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Name"" VARCHAR(255) NOT NULL,
                ""Type"" VARCHAR(10) NOT NULL,
                ""Value"" VARCHAR(255) NOT NULL,
                ""Ttl"" INTEGER NOT NULL DEFAULT 3600,
                ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UNIQUE (""Name"", ""Type"")
            );
            
            CREATE INDEX IF NOT EXISTS ""IX_DnsRecords_Name_Type"" ON ""DnsRecords"" (""Name"", ""Type"");";

        // Create CacheRecords table
        var cacheRecordsTable = @"
            CREATE TABLE IF NOT EXISTS ""CacheRecords"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Name"" VARCHAR(255) NOT NULL,
                ""Type"" VARCHAR(10) NOT NULL,
                ""Response"" TEXT NOT NULL,
                ""CachedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""ExpiresAt"" TIMESTAMP NOT NULL,
                ""UpstreamServer"" VARCHAR(255)
            );
            
            CREATE INDEX IF NOT EXISTS ""IX_CacheRecords_Name_Type"" ON ""CacheRecords"" (""Name"", ""Type"");
            CREATE INDEX IF NOT EXISTS ""IX_CacheRecords_ExpiresAt"" ON ""CacheRecords"" (""ExpiresAt"");";

        // Create Users table
        var usersTable = @"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Username"" VARCHAR(50) NOT NULL UNIQUE,
                ""PasswordHash"" VARCHAR(255) NOT NULL,
                ""Email"" VARCHAR(100) NOT NULL UNIQUE,
                ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""Role"" VARCHAR(50) NOT NULL DEFAULT 'User'
            );
            
            CREATE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");
            CREATE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");";

        // Create ServerConfigs table
        var serverConfigsTable = @"
            CREATE TABLE IF NOT EXISTS ""ServerConfigs"" (
                ""Key"" VARCHAR(255) PRIMARY KEY,
                ""Value"" TEXT NOT NULL,
                ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedBy"" VARCHAR(255) NOT NULL
            );";

        try
        {
            await connection.ExecuteAsync(dnsRecordsTable);
            await connection.ExecuteAsync(cacheRecordsTable);
            await connection.ExecuteAsync(usersTable);
            await connection.ExecuteAsync(serverConfigsTable);

            _logger.LogInformation("Database tables created/verified successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database tables");
            throw;
        }
    }
}
