# SiNS CLI (sns)

A command-line interface for managing the SiNS DNS Server via its REST API.

## Features

- **DNS Record Management**: Create, read, update, and delete DNS records
- **Cache Management**: View and clear DNS cache records
- **System Management**: Health checks, statistics, and configuration
- **User Management**: Authentication and user administration
- **Cross-Platform**: Works on Windows, macOS, and Linux
- **Modern CLI**: Built with System.CommandLine for excellent user experience

## Installation

### Prerequisites

- .NET 8.0 SDK or Runtime
- SiNS DNS Server running and accessible

### Build from Source

```bash
# Clone the repository
git clone https://github.com/judyandiealvarez/SiNS.git
cd sins

# Build the CLI tool
dotnet build sins-cli/sins-cli.csproj

# Run the CLI
dotnet run --project sins-cli/sins-cli.csproj -- --help
```

### Create Executable

```bash
# Publish as self-contained executable
dotnet publish sins-cli/sins-cli.csproj -c Release -r win-x64 --self-contained true
dotnet publish sins-cli/sins-cli.csproj -c Release -r linux-x64 --self-contained true
dotnet publish sins-cli/sins-cli.csproj -c Release -r osx-x64 --self-contained true
```

## Usage

### Global Options

- `--server <url>`: Server URL (default: http://localhost)
- `--token <token>`: Authentication token (or use SINS_TOKEN environment variable)

### Authentication

```bash
# Login to get a token
sins-cli auth login --username admin --password admin123

# Register a new user (Admin only)
sins-cli auth register --username newuser --password password123 --email user@example.com --role User

# List all users (Admin only)
sins-cli auth users
```

### DNS Record Management

```bash
# List all DNS records
sins-cli dns list

# List records filtered by type
sins-cli dns list --type A

# List records filtered by name
sins-cli dns list --name example.com

# Get a specific record
sins-cli dns get --id 1

# Add a new DNS record
sins-cli dns add --name example.com --type A --value 192.168.1.100 --ttl 3600

# Update an existing record
sins-cli dns update --id 1 --name example.com --type A --value 192.168.1.200 --ttl 7200

# Delete a DNS record
sins-cli dns delete --id 1
```

### Cache Management

```bash
# List all cache records
sins-cli cache list

# List cache records filtered by domain
sins-cli cache list --domain google.com

# List expired cache records
sins-cli cache list --expired true

# Clear all cache records
sins-cli cache clear-all

# Clear expired cache records only
sins-cli cache clear-expired
```

### System Management

```bash
# Check system health
sins-cli system health

# Show system statistics
sins-cli system stats

# Get current configuration
sins-cli system config get

# Update configuration
sins-cli system config update --cache-timeout 120 --upstream-servers 8.8.8.8,1.1.1.1

# Show server version
sins-cli system version
```

### Examples

#### Complete Workflow

```bash
# 1. Login to the server
sins-cli auth login --username admin --password admin123

# 2. Check server health
sins-cli system health

# 3. Add a DNS record
sins-cli dns add --name test.example.com --type A --value 192.168.1.100

# 4. List all records to verify
sins-cli dns list

# 5. Check cache
sins-cli cache list

# 6. View statistics
sins-cli system stats
```

#### Using Environment Variables

```bash
# Set the authentication token
export SINS_TOKEN="your-jwt-token-here"

# Use a different server
export SINS_SERVER="https://ns.home.net"

# Now you can run commands without specifying token/server
sins-cli dns list
sins-cli system health
```

#### Batch Operations

```bash
# Add multiple DNS records
sins-cli dns add --name web.example.com --type A --value 192.168.1.10
sins-cli dns add --name mail.example.com --type A --value 192.168.1.20
sins-cli dns add --name example.com --type CNAME --value web.example.com

# List all A records
sins-cli dns list --type A

# Clear cache and check stats
sins-cli cache clear-all
sins-cli system stats
```

## Configuration

### Environment Variables

- `SINS_TOKEN`: Authentication token (automatically set after login)
- `SINS_SERVER`: Default server URL

### Configuration File

You can create a configuration file at `~/.sins/config.json`:

```json
{
  "server": "https://ns.home.net",
  "token": "your-jwt-token-here",
  "defaultTtl": 3600
}
```

## Error Handling

The CLI provides clear error messages for common issues:

- **Authentication errors**: Invalid credentials or expired tokens
- **Permission errors**: Insufficient privileges for operations
- **Network errors**: Connection issues or server unavailable
- **Validation errors**: Invalid input data

## Output Format

The CLI provides formatted, colored output:

- ✅ Success messages
- ❌ Error messages
- ⚠️ Warning messages
- ℹ️ Information messages

Tables are formatted for easy reading with proper column alignment.

## Troubleshooting

### Common Issues

1. **Connection refused**: Check if the SiNS server is running
2. **Authentication failed**: Verify username/password or token validity
3. **Permission denied**: Ensure your user has the required role
4. **Invalid input**: Check command syntax and parameter values

### Debug Mode

Enable verbose logging:

```bash
# Set environment variable for debug output
export SINS_DEBUG=true

# Run commands to see detailed logs
sns system health
```

### Network Issues

```bash
# Test connectivity
curl -f http://localhost/api/dns/health

# Check if port is accessible
telnet localhost 80
```

## Development

### Building from Source

```bash
# Clone and build
git clone https://github.com/judyandiealvarez/SiNS.git
cd sins/sins-cli

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the CLI
dotnet run -- --help
```

### Project Structure

```
sins-cli/
├── Commands/           # Command implementations
│   ├── AuthCommands.cs
│   ├── DnsCommands.cs
│   ├── CacheCommands.cs
│   └── SystemCommands.cs
├── Models/             # API models
│   └── ApiModels.cs
├── Services/           # Business logic
│   ├── ApiClient.cs
│   └── OutputService.cs
├── Program.cs          # Entry point
└── sins-cli.csproj     # Project file
```

### Adding New Commands

1. Create a new command class in the `Commands/` directory
2. Implement the command logic
3. Register the command in `Program.cs`
4. Add appropriate models if needed

## License

This project is licensed under the same license as the main SiNS project.

## Contributing

Contributions are welcome! Please see the main project's contributing guidelines.

## Support

For issues and questions:

1. Check the troubleshooting section
2. Review the API documentation
3. Open an issue on GitHub
4. Check the main project documentation

