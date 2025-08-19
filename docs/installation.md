# Installation Guide

This guide covers installing the DNS server in various environments, from development to production.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Production Installation](#production-installation)
- [Development Installation](#development-installation)
- [Manual Installation](#manual-installation)
- [Post-Installation](#post-installation)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

#### Production Requirements
- **OS**: Linux (Ubuntu 20.04+, Debian 11+, CentOS 8+, RHEL 8+)
- **Architecture**: x86_64, ARM64
- **RAM**: 2GB minimum, 4GB recommended
- **Storage**: 10GB minimum, 20GB recommended
- **Network**: 100Mbps minimum, 1Gbps recommended

#### Development Requirements
- **OS**: Linux, macOS, Windows
- **Docker**: Docker Desktop or Docker Engine
- **RAM**: 4GB minimum
- **Storage**: 5GB available space

### Software Dependencies

#### Required Software
- **Docker**: Version 20.10+ (with Compose v2)
- **Git**: For cloning the repository
- **curl**: For health checks and testing

#### Optional Software
- **dig/nslookup**: For DNS testing
- **psql**: For direct database access
- **jq**: For JSON processing

### Network Requirements

#### Production Network
- **Port 53**: UDP and TCP (DNS queries)
- **Port 80**: HTTP (Web interface)
- **Port 443**: HTTPS (if using SSL/TLS)
- **Outbound**: Access to upstream DNS servers (8.8.8.8, 1.1.1.1)

#### Development Network
- **Port 80**: HTTP (Web interface)
- **Port 5354**: DNS (alternative to avoid conflicts)

## Production Installation

### Docker Hub Images

SiNS is available as pre-built Docker images on Docker Hub for easy deployment:

#### Available Image Tags
- `judyandiealvarez/sins:latest` - Latest stable version
- `judyandiealvarez/sins:1.0.6` - Specific version (replace with desired version)
- `judyandiealvarez/sins:1.0` - Latest 1.0.x version

#### Image Details
- **Base**: .NET 8.0 runtime
- **Size**: ~200MB
- **Architecture**: linux/amd64
- **Source**: https://github.com/judyandiealvarez/SiNS

#### Pull Image
```bash
# Pull latest version
docker pull judyandiealvarez/sins:latest

# Pull specific version
docker pull judyandiealvarez/sins:1.0.6
```

### Automated Installation

The recommended approach for production is using the automated deployment script with Docker Hub images.

#### Step 1: Prepare the System

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y  # Ubuntu/Debian
# OR
sudo yum update -y  # CentOS/RHEL

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

#### Step 2: Deploy Using Docker Hub (Recommended)

```bash
# Create deployment directory
mkdir sins-production && cd sins-production

# Download configuration files
curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/docker-compose.yml
curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/deploy.sh
chmod +x deploy.sh

# Run deployment (requires root)
sudo ./deploy.sh
```

#### Alternative: Clone Repository

```bash
# Clone the repository
git clone https://github.com/judyandiealvarez/SiNS.git
cd sins

# Make deployment script executable
chmod +x deploy.sh

# Run deployment (requires root)
sudo ./deploy.sh
```

#### Step 3: Verify Installation

```bash
# Check service status
docker-compose ps

# Test DNS resolution
dig @127.0.0.1 google.com

# Test web interface
curl http://localhost/api/dns/health

# Check logs
docker-compose logs dns-server
```

### Manual Production Installation

If you prefer manual installation or need custom configuration:

#### Step 1: Stop System DNS Services

```bash
# Stop systemd-resolved (Ubuntu/Debian)
sudo systemctl stop systemd-resolved
sudo systemctl disable systemd-resolved

# Stop other DNS services
sudo systemctl stop bind9 dnsmasq 2>/dev/null || true
sudo systemctl disable bind9 dnsmasq 2>/dev/null || true

# Check for other DNS services
sudo netstat -tulpn | grep :53
```

#### Step 2: Configure Network DNS

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

#### Step 3: Deploy DNS Server

```bash
# Clone repository
git clone <repository-url>
cd sins

# Build and start services
docker-compose up -d --build

# Wait for services to be ready
sleep 30

# Check status
docker-compose ps
```

## Development Installation

### Docker Desktop (macOS/Windows)

#### Step 1: Install Docker Desktop

Download and install Docker Desktop from [docker.com](https://www.docker.com/products/docker-desktop).

#### Step 2: Clone and Start

```bash
# Clone repository
git clone <repository-url>
cd sins

# Start services
docker-compose up -d

# Access web interface
open http://localhost  # macOS
# OR
start http://localhost  # Windows
```

#### Step 3: Test DNS (Development Port)

```bash
# Test DNS on development port
dig @localhost -p 5354 google.com
nslookup google.com localhost 5354
```

### Linux Development

#### Step 1: Install Docker

```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

#### Step 2: Deploy

```bash
# Clone repository
git clone <repository-url>
cd sins

# Start services
docker-compose up -d

# Test
curl http://localhost/api/dns/health
```

## Manual Installation

### Building from Source

If you need to build from source or customize the build:

#### Step 1: Install .NET 8 SDK

```bash
# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# CentOS/RHEL
sudo dnf install dotnet-sdk-8.0
```

#### Step 2: Install PostgreSQL

```bash
# Ubuntu/Debian
sudo apt install postgresql postgresql-contrib

# CentOS/RHEL
sudo dnf install postgresql postgresql-server
sudo postgresql-setup initdb
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

#### Step 3: Build Application

```bash
# Clone repository
git clone <repository-url>
cd sins

# Restore dependencies
dotnet restore

# Build application
dotnet build -c Release

# Run database migrations
dotnet ef database update

# Run application
dotnet run
```

## Post-Installation

### Initial Configuration

#### Step 1: Access Web Interface

1. Open browser and navigate to `http://localhost`
2. Login with default credentials:
   - **Username**: admin
   - **Password**: admin123

#### Step 2: Change Default Password

1. Go to User Management section
2. Edit the admin user
3. Set a strong password

#### Step 3: Configure DNS Settings

1. Go to Settings section
2. Configure:
   - Cache timeout (default: 60 minutes)
   - Upstream DNS servers
   - DNS ports

### Security Hardening

#### Step 1: Update JWT Key

Edit `sins/appsettings.json`:

```json
{
  "Jwt": {
    "Key": "your-super-secret-jwt-key-change-this-in-production"
  }
}
```

#### Step 2: Update Database Password

```bash
# Connect to PostgreSQL
docker exec -it dns-postgres psql -U postgres -d dns_server

# Change password
ALTER USER postgres PASSWORD 'new-secure-password';
```

#### Step 3: Configure Firewall

```bash
# Allow DNS traffic
sudo ufw allow 53/tcp
sudo ufw allow 53/udp

# Allow web interface
sudo ufw allow 80/tcp

# Enable firewall
sudo ufw enable
```

### Performance Tuning

#### Step 1: Configure Resource Limits

Edit `docker-compose.yml`:

```yaml
services:
  dns-server:
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '1.0'
        reservations:
          memory: 1G
          cpus: '0.5'
```

#### Step 2: Database Optimization

```bash
# Connect to database
docker exec -it dns-postgres psql -U postgres -d dns_server

# Create indexes for better performance
CREATE INDEX idx_dns_records_name ON dns_records(name);
CREATE INDEX idx_cache_records_domain ON cache_records(domain);
CREATE INDEX idx_cache_records_expires ON cache_records(expires_at);
```

## Troubleshooting

### Common Issues

#### Port 53 Already in Use

```bash
# Check what's using port 53
sudo netstat -tulpn | grep :53

# Stop conflicting services
sudo systemctl stop systemd-resolved
sudo systemctl stop bind9
```

#### Docker Permission Issues

```bash
# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker

# Or run with sudo
sudo docker-compose up -d
```

#### Database Connection Issues

```bash
# Check PostgreSQL logs
docker-compose logs postgres

# Test database connection
docker exec -it dns-postgres psql -U postgres -d dns_server -c "SELECT 1;"
```

#### DNS Resolution Not Working

```bash
# Check DNS server logs
docker-compose logs dns-server

# Test DNS resolution
dig @127.0.0.1 google.com

# Check container status
docker-compose ps
```

### Log Analysis

#### View Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs dns-server
docker-compose logs postgres

# Follow logs in real-time
docker-compose logs -f
```

#### Common Log Messages

- `DNS query received`: Normal DNS query processing
- `Cache hit`: Query served from cache
- `Cache miss`: Query forwarded to upstream
- `Database connection failed`: Database connectivity issue
- `Health check failed`: Service health issue

### Recovery Procedures

#### Service Recovery

```bash
# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart dns-server

# Rebuild and restart
docker-compose down
docker-compose up -d --build
```

#### Database Recovery

```bash
# Backup database
docker exec dns-postgres pg_dump -U postgres dns_server > backup.sql

# Restore database
docker exec -i dns-postgres psql -U postgres dns_server < backup.sql
```

#### Network Recovery

```bash
# Recreate network
docker-compose down
docker network prune -f
docker-compose up -d
```

## Next Steps

After successful installation:

1. **Configure DNS Records**: Add your domain records
2. **Set Up Monitoring**: Configure health checks and alerts
3. **Backup Strategy**: Set up regular database backups
4. **Security Review**: Review and harden security settings
5. **Performance Tuning**: Optimize for your workload

For more detailed information, see:
- [Configuration Guide](configuration.md)
- [Web Interface Guide](web-interface.md)
- [Monitoring Guide](monitoring.md)
