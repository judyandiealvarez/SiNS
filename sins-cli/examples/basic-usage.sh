#!/bin/bash

# SiNS CLI Basic Usage Examples
# This script demonstrates how to use the SiNS CLI tool

echo "=== SiNS CLI Basic Usage Examples ==="
echo

# Set the server URL (change this to your actual server)
SERVER_URL="http://localhost"

echo "1. Checking server health..."
dotnet run -- --server $SERVER_URL system health
echo

echo "2. Getting server version..."
dotnet run -- --server $SERVER_URL system version
echo

echo "3. Getting server statistics..."
dotnet run -- --server $SERVER_URL system stats
echo

echo "4. Getting current configuration..."
dotnet run -- --server $SERVER_URL system config get
echo

echo "5. Listing DNS records..."
dotnet run -- --server $SERVER_URL dns list
echo

echo "6. Listing cache records..."
dotnet run -- --server $SERVER_URL cache list
echo

echo "=== Authentication Examples ==="
echo

echo "7. Login (replace with your credentials)..."
echo "dotnet run -- --server $SERVER_URL auth login --username admin --password admin123"
echo

echo "8. After login, you can use the token for authenticated operations:"
echo "export SINS_TOKEN='your-jwt-token-here'"
echo "dotnet run -- --server $SERVER_URL dns list"
echo

echo "=== DNS Management Examples ==="
echo

echo "9. Adding a DNS record (requires authentication):"
echo "dotnet run -- --server $SERVER_URL dns add --name test.example.com --type A --value 192.168.1.100"
echo

echo "10. Updating a DNS record (requires authentication):"
echo "dotnet run -- --server $SERVER_URL dns update --id 1 --name test.example.com --type A --value 192.168.1.200"
echo

echo "11. Deleting a DNS record (requires authentication):"
echo "dotnet run -- --server $SERVER_URL dns delete --id 1"
echo

echo "=== Cache Management Examples ==="
echo

echo "12. Clearing all cache (requires authentication):"
echo "dotnet run -- --server $SERVER_URL cache clear-all"
echo

echo "13. Clearing expired cache (requires authentication):"
echo "dotnet run -- --server $SERVER_URL cache clear-expired"
echo

echo "=== Configuration Examples ==="
echo

echo "14. Updating server configuration (requires authentication):"
echo "dotnet run -- --server $SERVER_URL system config update --cache-timeout 120 --upstream-servers 8.8.8.8,1.1.1.1"
echo

echo "=== Filtering Examples ==="
echo

echo "15. List only A records:"
echo "dotnet run -- --server $SERVER_URL dns list --type A"
echo

echo "16. List records containing 'example':"
echo "dotnet run -- --server $SERVER_URL dns list --name example"
echo

echo "17. List cache records for a specific domain:"
echo "dotnet run -- --server $SERVER_URL cache list --domain google.com"
echo

echo "=== Environment Variables ==="
echo

echo "You can also use environment variables:"
echo "export SINS_SERVER='$SERVER_URL'"
echo "export SINS_TOKEN='your-jwt-token-here'"
echo "dotnet run -- dns list  # No need to specify --server and --token"
echo

echo "=== Complete Workflow Example ==="
echo

echo "Complete workflow with authentication:"
echo "1. Login: dotnet run -- --server $SERVER_URL auth login --username admin --password admin123"
echo "2. Save token: export SINS_TOKEN='token-from-login'"
echo "3. Add record: dotnet run -- --server $SERVER_URL dns add --name web.example.com --type A --value 192.168.1.10"
echo "4. List records: dotnet run -- --server $SERVER_URL dns list"
echo "5. Check stats: dotnet run -- --server $SERVER_URL system stats"
echo

echo "=== Notes ==="
echo "- Replace 'admin' and 'admin123' with your actual credentials"
echo "- Change the server URL to match your SiNS server"
echo "- Most operations require authentication (Admin role for write operations)"
echo "- The CLI supports both command-line options and environment variables"
echo "- Use --help on any command for detailed usage information"

