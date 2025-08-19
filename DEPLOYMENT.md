# Production Deployment Guide

This guide covers deploying the DNS server in a production Linux environment.

## Prerequisites

- Linux system (Ubuntu 20.04+ / Debian 11+ / CentOS 8+)
- Docker and Docker Compose installed
- Root access (required for port 53 and system DNS management)
- Network access for Docker image downloads

## Docker Hub Images

SiNS is available as pre-built Docker images on Docker Hub:

### Available Tags
- `judyandiealvarez/sins:latest` - Latest stable version
- `judyandiealvarez/sins:1.0.6` - Specific version (replace with desired version)
- `judyandiealvarez/sins:1.0` - Latest 1.0.x version

### Pull Image
```bash
# Pull latest version
docker pull judyandiealvarez/sins:latest

# Pull specific version
docker pull judyandiealvarez/sins:1.0.6
```

### Image Information
- **Base**: .NET 8.0 runtime
- **Size**: ~200MB
- **Architecture**: linux/amd64
- **Source**: https://github.com/judyandiealvarez/SiNS

## Quick Deployment

### Option 1: Using Docker Hub Image (Recommended)

1. **Create deployment directory and download files**:
   ```bash
   mkdir sins-production && cd sins-production
   curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/docker-compose.yml
   curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/deploy.sh
   chmod +x deploy.sh
   ```

2. **Run the deployment script**:
   ```bash
   sudo ./deploy.sh
   ```

### Option 2: Clone Repository

1. **Clone the repository**:
   ```bash
   git clone https://github.com/judyandiealvarez/SiNS.git
   cd sins
   ```

2. **Run the deployment script**:
   ```bash
   sudo ./deploy.sh
   ```

The script will automatically:
- Stop system DNS services
- Configure network DNS settings
- Deploy the DNS server with static IPs
- Test the deployment
- Show status information

## Manual Deployment

If you prefer manual deployment:

### 1. Stop System DNS Services

```bash
# Stop systemd-resolved (Ubuntu/Debian)
sudo systemctl stop systemd-resolved
sudo systemctl disable systemd-resolved

# Stop other DNS services
sudo systemctl stop bind9 dnsmasq 2>/dev/null || true
sudo systemctl disable bind9 dnsmasq 2>/dev/null || true
```

### 2. Configure Network DNS

```bash
# Backup current DNS configuration
sudo cp /etc/resolv.conf /etc/resolv.conf.backup

# Set localhost as primary DNS
echo "nameserver 127.0.0.1" | sudo tee /etc/resolv.conf
echo "nameserver 8.8.8.8" | sudo tee -a /etc/resolv.conf
echo "nameserver 1.1.1.1" | sudo tee -a /etc/resolv.conf

# Prevent automatic changes
sudo chattr +i /etc/resolv.conf
```

### 3. Deploy DNS Server

```bash
# Start services using Docker Hub image
docker-compose up -d

# Wait for services to be healthy
docker-compose ps
```

**Note**: The deployment uses the pre-built Docker Hub image `judyandiealvarez/sins:latest`. For specific versions, you can modify the image tag in `docker-compose.yml`:
- `judyandiealvarez/sins:latest` - Latest stable version
- `judyandiealvarez/sins:1.0.6` - Specific version
- `judyandiealvarez/sins:1.0` - Latest 1.0.x version

### 4. Test Deployment

```bash
# Test DNS resolution
nslookup google.com 127.0.0.1
dig @127.0.0.1 google.com

# Test web interface
curl http://localhost/api/dns/health
```

## Network Configuration

### Static IP Addresses
- **PostgreSQL**: 172.20.0.2
- **DNS Server**: 172.20.0.3
- **Network**: 172.20.0.0/16

### Port Mappings
- **Web Interface**: 80 → 80
- **DNS UDP**: 53 → 53
- **DNS TCP**: 53 → 53

## Service Management

### Start Services
```bash
docker-compose up -d
```

### Stop Services
```bash
docker-compose down
```

### View Logs
```bash
# DNS server logs
docker-compose logs dns-server

# PostgreSQL logs
docker-compose logs postgres

# All logs
docker-compose logs -f
```

### Restart Services
```bash
docker-compose restart
```

## Health Monitoring

### Check Service Status
```bash
docker-compose ps
```

### Health Check Endpoints
- **DNS Server**: `curl http://localhost/api/dns/health`
- **PostgreSQL**: `docker exec dns-postgres pg_isready -U postgres`

### Automatic Restart
Services are configured with `restart: unless-stopped` and health checks for automatic recovery.

## Troubleshooting

### DNS Resolution Issues
```bash
# Check if DNS server is running
docker-compose ps

# Test DNS resolution
dig @127.0.0.1 google.com

# Check DNS server logs
docker-compose logs dns-server
```

### Database Issues
```bash
# Check PostgreSQL status
docker-compose logs postgres

# Connect to database
docker exec -it dns-postgres psql -U postgres -d dns_server
```

### Web Interface Issues
```bash
# Check web interface
curl http://localhost

# Check container health
docker-compose ps
```

### Network Issues
```bash
# Check network configuration
docker network ls
docker network inspect sins_dns-network

# Check static IPs
docker exec dns-server ip addr show
docker exec dns-postgres ip addr show
```

## Restore System DNS

If you need to restore the original system DNS:

```bash
# Stop DNS server
docker-compose down

# Restore system DNS services
sudo systemctl enable systemd-resolved
sudo systemctl start systemd-resolved

# Restore resolv.conf
sudo chattr -i /etc/resolv.conf
sudo cp /etc/resolv.conf.backup /etc/resolv.conf
```

## Security Considerations

### Default Credentials
- **Web Interface**: admin / admin123
- **Database**: postgres / postgres

**Important**: Change these credentials in production!

### Network Security
- The DNS server runs on privileged ports (53)
- Static IP addressing prevents IP conflicts
- Container isolation provides security boundaries

### JWT Configuration
Update the JWT key in `sins/appsettings.json`:
```json
{
  "Jwt": {
    "Key": "your-super-secret-jwt-key-change-this-in-production"
  }
}
```

## Performance Tuning

### Database Optimization
```bash
# Connect to PostgreSQL
docker exec -it dns-postgres psql -U postgres -d dns_server

# Check performance
SELECT * FROM pg_stat_database;
```

### DNS Cache Configuration
- Default cache timeout: 60 minutes
- Configurable via web interface
- Automatic cleanup of expired records

### Resource Limits
Monitor resource usage:
```bash
docker stats
```

## Backup and Recovery

### Database Backup
```bash
# Create backup
docker exec dns-postgres pg_dump -U postgres dns_server > backup.sql

# Restore backup
docker exec -i dns-postgres psql -U postgres dns_server < backup.sql
```

### Configuration Backup
```bash
# Backup configuration
cp -r sins/appsettings.json backup/
cp -r docker-compose.yml backup/
```

## Monitoring

### Log Monitoring
```bash
# Follow logs in real-time
docker-compose logs -f

# Filter DNS queries
docker-compose logs dns-server | grep "DNS query"
```

### Metrics
- Cache hit rates available in web interface
- DNS query statistics
- Service health status

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review the logs
3. Verify network configuration
4. Test DNS resolution manually
