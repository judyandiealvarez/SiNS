# SiNS Project Context & Architecture Documentation

## Table of Contents

1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [Technology Stack](#technology-stack)
4. [Deployment Architecture](#deployment-architecture)
5. [CI/CD Pipeline](#cicd-pipeline)
6. [Database Design](#database-design)
7. [Security Architecture](#security-architecture)
8. [Network Configuration](#network-configuration)
9. [Production Infrastructure](#production-infrastructure)
10. [Development Workflow](#development-workflow)
11. [Troubleshooting & Maintenance](#troubleshooting--maintenance)
12. [Future Roadmap](#future-roadmap)

---

## Project Overview

### What is SiNS?

**SiNS** stands for **[Si]mple [N]ame [S]erver** - a complete DNS server solution with web-based management interface.

### Core Concept

SiNS is a hybrid authoritative and recursive DNS server that combines:
- **Traditional DNS functionality** (UDP/TCP on port 53)
- **Modern web management interface** (Vue.js frontend)
- **Database-driven configuration** (PostgreSQL backend)
- **Containerized deployment** (Docker/Docker Compose)

### Key Features

- **DNS Server**: Authoritative and recursive DNS with caching
- **Web Management**: Modern Vue.js interface with Vuex state management
- **Database Storage**: PostgreSQL for DNS records, cache, and configuration
- **Authentication**: JWT-based authentication with role-based access
- **Real-time Configuration**: Database-driven configuration with immediate effect
- **Production Ready**: Static IP addressing and proper service management
- **HTTPS Support**: HAProxy for SSL termination
- **Version Display**: Build number integration in web UI
- **Authoritative DNSSEC**: Zone signing (algorithm 13), NSEC, DS export API ([docs/dnssec.md](docs/dnssec.md))

---

## System Architecture

### High-Level Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   DNS Client    │    │   Web Browser   │    │  Upstream DNS   │
│                 │    │                 │    │    Servers      │
└─────────┬───────┘    └─────────┬───────┘    └─────────┬───────┘
          │                      │                      │
          │ UDP/TCP:53           │ HTTP:80/HTTPS:443    │ UDP:53
          │                      │                      │
    ┌─────▼──────────────────────▼──────────────────────▼─────┐
    │                  SiNS DNS Server                        │
    │  ┌─────────────────┐  ┌─────────────────┐              │
    │  │   DNS Engine    │  │   Web API       │              │
    │  │                 │  │                 │              │
    │  │ • UDP/TCP       │  │ • Authentication│              │
    │  │ • Caching       │  │ • Record Mgmt   │              │
    │  │ • Recursion     │  │ • Statistics    │              │
    │  │ • Response Gen  │  │ • Configuration │              │
    │  └─────────────────┘  └─────────────────┘              │
    └─────────────────────────────────────────────────────────┘
                              │
                              │ Database Connection
                              │
                    ┌─────────▼─────────┐
                    │    PostgreSQL     │
                    │                   │
                    │ • DNS Records     │
                    │ • Cache Records   │
                    │ • Users           │
                    │ • Configuration   │
                    └───────────────────┘
```

### Component Architecture

#### 1. DNS Engine (DnsServer.cs)
- **UDP Listener**: Handles UDP DNS queries on port 53
- **TCP Listener**: Handles TCP DNS queries on port 53
- **Query Parser**: Parses incoming DNS queries
- **Response Generator**: Creates DNS responses
- **Cache Manager**: Manages DNS response caching
- **Recursive Resolver**: Resolves queries from upstream servers

#### 2. Web API (Controllers/)
- **AuthController**: JWT authentication and user management
- **DnsController**: DNS record CRUD operations and cache management
- **DnssecController**: DNSSEC zone CRUD, DS and DNSKEY export (`/api/dnssec/*`)
- **Configuration Controller**: Server configuration management

#### 3. Web Interface (wwwroot/)
- **Vue.js Frontend**: Modern JavaScript framework
- **Vuex State Management**: Centralized state management
- **Bootstrap UI**: Responsive design framework
- **Real-time Updates**: Live data updates

#### 4. Database Layer (Data/)
- **PostgreSQL**: Primary data store
- **Entity Framework Core**: Object-relational mapping
- **Database Context**: Database connection and configuration

---

## Technology Stack

### Backend Technologies
- **.NET 8**: Modern, cross-platform framework
- **ASP.NET Core**: Web framework for building APIs
- **Entity Framework Core**: Object-relational mapping
- **PostgreSQL**: Relational database
- **JWT**: JSON Web Token authentication
- **BCrypt**: Password hashing

### Frontend Technologies
- **Vue.js 3**: Progressive JavaScript framework
- **Vuex**: State management for Vue.js
- **Bootstrap 5**: CSS framework for responsive design
- **Axios**: HTTP client for API communication

### Infrastructure Technologies
- **Docker**: Containerization platform
- **Docker Compose**: Multi-container orchestration
- **HAProxy**: Load balancer and SSL termination
- **Linux**: Operating system (Ubuntu/Debian)

### Development Tools
- **Git**: Version control
- **GitHub Actions**: CI/CD automation
- **Visual Studio Code**: Development environment
- **Postman**: API testing

---

## Deployment Architecture

### Container Architecture

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:15
    container_name: dns-postgres
    networks:
      dns-network:
        ipv4_address: 172.20.0.2
        ipv6_address: 2001:db8::2

  dns-server:
    image: swipentap/sins:latest
    container_name: dns-server
    ports:
      - "80:80"
      - "53:53/udp"
      - "53:53/tcp"
    networks:
      dns-network:
        ipv4_address: 172.20.0.3
        ipv6_address: 2001:db8::3
    privileged: true
    cap_add:
      - NET_BIND_SERVICE
```

### Network Configuration

#### Static IP Addressing
- **Subnet**: 172.20.0.0/16 (IPv4), 2001:db8::/64 (IPv6)
- **Gateway**: 172.20.0.1 (IPv4), 2001:db8::1 (IPv6)
- **PostgreSQL**: 172.20.0.2 (IPv4), 2001:db8::2 (IPv6)
- **DNS Server**: 172.20.0.3 (IPv4), 2001:db8::3 (IPv6)

#### Port Mappings
- **53/UDP**: DNS queries (UDP)
- **53/TCP**: DNS queries (TCP)
- **80/TCP**: Web interface and API
- **443/TCP**: HTTPS (HAProxy)

### Production Deployment

#### Current Production Setup
- **Server**: 10.11.2.5 (Ubuntu/Debian)
- **Self-hosted Runner**: GitHub Actions runner for deployment
- **HAProxy**: SSL termination with wildcard certificates
- **Domain**: ns.home.net

#### Deployment Process
1. **CI Pipeline**: Build and test on `docker-build` runner (10.11.2.7)
2. **Docker Hub**: Push image to `swipentap/sins:latest`
3. **Self-hosted Deployment**: Deploy to production server (10.11.2.5)
4. **Health Checks**: Verify DNS and web interface functionality

---

## CI/CD Pipeline

### GitHub Actions Workflows

#### 1. CI Pipeline (.github/workflows/ci.yml)
```yaml
jobs:
  test:
    runs-on: docker-build  # Self-hosted runner on 10.11.2.7
    steps:
      - dotnet restore
      - dotnet build
      - dotnet test

  build-docker:
    runs-on: docker-build
    steps:
      - Build Docker image with version info
      - Push to Docker Hub (swipentap/sins)

  lint:
    runs-on: docker-build
    steps:
      - dotnet format verification
      - Dockerfile linting (Hadolint)
```

#### 2. Deployment Pipeline (.github/workflows/deploy-self-hosted.yml)
```yaml
jobs:
  deploy:
    runs-on: self-hosted  # Runner on 10.11.2.5
    steps:
      - Checkout code
      - Deploy SiNS with comprehensive testing
      - Health checks and validation
```

### Build Process

#### Docker Image Building
```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
FROM build AS publish
FROM base AS final

# Version integration
ARG BUILD_NUMBER=0
ARG APP_VERSION=1.0.0
ENV BUILD_NUMBER=$BUILD_NUMBER
ENV APP_VERSION=$APP_VERSION
```

#### Version Display
- **Format**: `1.0.0.{build_number}`
- **Source**: GitHub Actions `${{ github.run_number }}`
- **Display**: Web UI shows version in sidebar

### Self-hosted Runners

#### Build Runner (10.11.2.7)
- **Purpose**: CI/CD builds and Docker image creation
- **Label**: `docker-build`
- **User**: `jaal` (member of docker group)
- **Services**: Docker daemon, systemd-resolved

#### Deployment Runner (10.11.2.5)
- **Purpose**: Production deployment
- **Label**: `self-hosted`
- **User**: `jaal` (with sudo NOPASSWD access)
- **Services**: Docker daemon, HAProxy

---

## Database Design

### Entity Relationship Diagram

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     Users       │    │   DnsRecords    │    │  CacheRecords   │
│                 │    │                 │    │                 │
│ • id (PK)       │    │ • id (PK)       │    │ • id (PK)       │
│ • username      │    │ • name          │    │ • domain        │
│ • email         │    │ • type          │    │ • type          │
│ • password_hash │    │ • value         │    │ • response      │
│ • role          │    │ • ttl           │    │ • resolved_ips  │
│ • is_active     │    │ • is_active     │    │ • upstream_srv  │
│ • created_at    │    │ • created_at    │    │ • expires_at    │
│                 │    │ • updated_at    │    │ • updated_by    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                │
                       ┌─────────────────┐
                       │  ServerConfig   │
                       │                 │
                       │ • key (PK)      │
                       │ • value         │
                       │ • updated_at    │
                       │ • updated_by    │
                       └─────────────────┘
```

### Database Schema

#### Users Table
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'User',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### DnsRecords Table
```sql
CREATE TABLE dns_records (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(10) NOT NULL,
    value TEXT NOT NULL,
    ttl INTEGER NOT NULL DEFAULT 3600,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(name, type)
);
```

#### CacheRecords Table
```sql
CREATE TABLE cache_records (
    id SERIAL PRIMARY KEY,
    domain VARCHAR(255) NOT NULL,
    type VARCHAR(10) NOT NULL,
    response BYTEA NOT NULL,
    resolved_ips TEXT[],
    upstream_server VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL
);
```

#### ServerConfig Table
```sql
CREATE TABLE server_config (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by VARCHAR(50) NOT NULL
);
```

### Soft Delete Implementation

#### Design Decision
- **Soft Delete**: Records marked as `is_active = false` instead of physical deletion
- **Reason**: Data recovery, audit trail, and constraint protection
- **Impact**: Unique constraints prevent duplicate names even for inactive records

#### Implementation
```csharp
// Delete operation (soft delete)
record.IsActive = false;
record.UpdatedAt = DateTime.UtcNow;

// Query only active records
var records = await _context.DnsRecords
    .Where(r => r.IsActive)
    .ToListAsync();
```

---

## Security Architecture

### Authentication & Authorization

#### JWT Token Structure
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user_id",
    "username": "admin",
    "role": "Admin",
    "iat": 1640995200,
    "exp": 1640998800
  }
}
```

#### Role-Based Access Control

- **Admin Role**:
  - Full access to all features
  - Can manage DNS records
  - Can manage users
  - Can configure server settings
  - Can manage cache

- **User Role**:
  - Read-only access to DNS records
  - Read-only access to cache
  - Can view statistics
  - Cannot modify configuration

### Data Security

#### Password Security
- **Hashing**: BCrypt with salt
- **Cost Factor**: 12 rounds
- **Storage**: Hashed passwords only

#### Database Security
- **Connection Encryption**: SSL/TLS
- **Access Control**: Role-based permissions
- **Input Validation**: Parameterized queries

#### Network Security
- **Container Isolation**: Network namespace isolation
- **Port Security**: Only necessary ports exposed
- **Static IPs**: Predictable network configuration

### HTTPS Implementation

#### HAProxy Configuration
```haproxy
frontend https_frontend
    bind *:443 ssl crt /etc/ssl/certs/certa-wildcard.pem
    
    acl host_ns_home hdr(host) -i ns.home.net
    use_backend sins_backend if host_ns_home
    default_backend sins_backend

backend sins_backend
    balance roundrobin
    server sins 127.0.0.1:80
```

#### SSL Certificate
- **Source**: Wildcard certificate from 10.11.2.3
- **Domain**: *.home.net
- **Location**: /etc/ssl/certs/certa-wildcard.pem

---

## Network Configuration

### Container Network

```
┌─────────────────────────────────────────────────────────────┐
│                    Docker Network                           │
│                    172.20.0.0/16                           │
│                                                             │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │   PostgreSQL    │    │   DNS Server    │                │
│  │   172.20.0.2    │    │   172.20.0.3    │                │
│  │   Port 5432     │    │   Port 53,80    │                │
│  └─────────────────┘    └─────────────────┘                │
└─────────────────────────────────────────────────────────────┘
```

### Port Management

#### System DNS Services
- **systemd-resolved**: Stopped and disabled on production
- **Port 53**: Freed for SiNS DNS server
- **Configuration**: Managed by deployment script

#### Port Bindings
- **53/UDP**: DNS queries (UDP)
- **53/TCP**: DNS queries (TCP)
- **80/TCP**: Web interface and API
- **443/TCP**: HTTPS (HAProxy)

### IPv6 Support

#### Dual Stack Configuration
- **IPv4 Subnet**: 172.20.0.0/16
- **IPv6 Subnet**: 2001:db8::/64
- **Gateway**: 172.20.0.1 (IPv4), 2001:db8::1 (IPv6)

---

## Production Infrastructure

### Current Production Setup

#### Server Configuration
- **Host**: 10.11.2.5
- **OS**: Ubuntu/Debian
- **User**: jaal (with sudo NOPASSWD access)
- **Services**: Docker, HAProxy, systemd-resolved (disabled)

#### Deployment Directory
```
/home/jaal/ci/
├── docker-compose.yml
├── deploy.log
└── haproxy.cfg
```

#### HAProxy Setup
- **SSL Termination**: Port 443
- **Backend**: 127.0.0.1:80 (SiNS web interface)
- **Certificate**: /etc/ssl/certs/certa-wildcard.pem

### Monitoring & Health Checks

#### Service Health Checks
```yaml
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U postgres -d dns_server"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

#### Deployment Validation
- **DNS Test**: `nslookup google.com 127.0.0.1`
- **Web Interface Test**: `curl -fs http://127.0.0.1/`
- **Retry Logic**: 3 attempts with 5-second delays

### Backup & Recovery

#### Database Backup
```bash
# Backup PostgreSQL data
docker exec dns-postgres pg_dump -U postgres dns_server > backup.sql

# Restore from backup
docker exec -i dns-postgres psql -U postgres dns_server < backup.sql
```

#### Configuration Backup
- **Docker Compose**: Version controlled in repository
- **HAProxy Config**: Manual backup recommended
- **SSL Certificates**: Backup from source server

---

## Development Workflow

### Local Development

#### Prerequisites
- **Docker Desktop**: Local container environment
- **.NET 8 SDK**: For local development
- **PostgreSQL**: Local database (optional)

#### Development Commands
```bash
# Build and run locally
docker-compose up -d

# View logs
docker-compose logs -f dns-server

# Test DNS functionality
dig @localhost google.com
nslookup google.com localhost

# Test web interface
curl http://localhost/api/dns/health
```

### Code Structure

#### Backend Structure
```
sins/
├── Controllers/          # API controllers
│   ├── AuthController.cs
│   └── DnsController.cs
├── Data/                # Database context
│   └── DnsContext.cs
├── Models/              # Entity models
│   ├── DnsRecord.cs
│   ├── User.cs
│   └── CacheRecord.cs
├── Services/            # Business logic
│   ├── DnsServer.cs
│   ├── AuthService.cs
│   └── ConfigurationService.cs
└── wwwroot/             # Frontend files
    ├── index.html
    └── app.js
```

#### Frontend Structure
```javascript
// Vue.js application structure
const app = new Vue({
  el: '#app',
  store,
  data: {
    // Application state
  },
  methods: {
    // Application methods
  }
});

// Vuex store
const store = new Vuex.Store({
  state: {
    records: [],
    users: [],
    cache: [],
    user: null,
    version: '1.0.0.0'
  },
  mutations: {
    // State mutations
  },
  actions: {
    // Async actions
  }
});
```

### Testing Strategy

#### API Testing
```bash
# Test authentication
curl -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Test DNS record creation
curl -X POST http://localhost/api/dns/records \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"test.example.com","type":"A","value":"192.168.1.100"}'
```

#### DNS Testing
```bash
# Test DNS resolution
dig @localhost test.example.com
nslookup test.example.com localhost

# Test cache
curl http://localhost/api/dns/cache
```

---

## Troubleshooting & Maintenance

### Common Issues

#### DNS Resolution Problems
```bash
# Check DNS server status
docker-compose ps
docker-compose logs dns-server

# Test DNS resolution
nslookup google.com 127.0.0.1
dig @127.0.0.1 google.com

# Check port 53
sudo netstat -tulpn | grep :53
```

#### Web Interface Issues
```bash
# Check web interface
curl -v http://localhost/
curl -v https://ns.home.net/

# Check HAProxy
sudo systemctl status haproxy
sudo journalctl -u haproxy -f
```

#### Database Issues
```bash
# Check PostgreSQL
docker-compose logs postgres
docker exec -it dns-postgres psql -U postgres -d dns_server

# Check database connectivity
docker exec dns-postgres pg_isready -U postgres -d dns_server
```

### Deployment Issues

#### Port 53 Conflicts
```bash
# Stop systemd-resolved
sudo systemctl stop systemd-resolved
sudo systemctl disable systemd-resolved

# Check what's using port 53
sudo lsof -i :53
sudo fuser -n udp 53
```

#### Permission Issues
```bash
# Add user to docker group
sudo usermod -aG docker jaal

# Configure sudo access
echo "jaal ALL=(ALL) NOPASSWD: ALL" | sudo tee /etc/sudoers.d/jaal
```

### Log Analysis

#### Application Logs
```bash
# View application logs
docker-compose logs -f dns-server

# View specific log levels
docker-compose logs dns-server | grep ERROR
docker-compose logs dns-server | grep WARNING
```

#### System Logs
```bash
# View system logs
sudo journalctl -f

# View Docker logs
sudo journalctl -u docker.service -f
```

### Performance Monitoring

#### Resource Usage
```bash
# Check container resource usage
docker stats

# Check system resources
htop
df -h
free -h
```

#### DNS Performance
```bash
# Test DNS response time
time dig @127.0.0.1 google.com

# Check cache hit rate
curl http://localhost/api/dns/stats
```

---

## CLI Tool Building and Deployment

### Overview

The SiNS CLI tool (`sns`) is a command-line interface for managing the DNS server via its REST API. It's built in C# using .NET 9.0 and deployed as a Debian package to a custom APT repository.

### CLI Architecture

#### Technology Stack
- **Language**: C# (.NET 9.0)
- **Framework**: System.CommandLine for CLI parsing
- **HTTP Client**: HttpClient for API communication
- **Serialization**: System.Text.Json for JSON handling
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

#### Project Structure
```
sins-cli/
├── Commands/           # CLI command implementations
│   ├── AuthCommands.cs
│   ├── DnsCommands.cs
│   ├── CacheCommands.cs
│   └── SystemCommands.cs
├── Services/           # Business logic services
│   ├── ApiClient.cs    # HTTP client for API calls
│   └── OutputService.cs # Output formatting
├── Models/             # Data models
├── debian/             # Debian packaging files
│   ├── control         # Package metadata
│   ├── postinst        # Post-installation script
│   └── prerm           # Pre-removal script
├── build-package.sh    # Package building script
└── sins-cli.csproj     # Project file
```

### Building the CLI

#### Prerequisites
```bash
# Install .NET 9.0 SDK
# Install Debian packaging tools
sudo apt install dpkg-dev build-essential fakeroot
```

#### Local Build Process
```bash
cd sins-cli

# Fix code formatting (important for CI/CD)
dotnet format --verbosity quiet

# Build the application
dotnet restore
dotnet build -c Release

# Test the CLI
dotnet run -- --help
```

#### Package Building
```bash
# Build Debian package
chmod +x build-package.sh
./build-package.sh 1.0.0

# Verify package
dpkg -c sns_1.0.0_amd64.deb
dpkg -I sns_1.0.0_amd64.deb
```

### CI/CD Pipeline

#### GitHub Actions Workflow
The CLI uses a GitHub Actions workflow (`.github/workflows/build-and-deploy-cli.yml`) that:

1. **Triggers**: On push to tags matching `cli-v*`
2. **Build Job** (runs on `docker-build` runner):
   - Sets up .NET 9.0 environment
   - Installs Debian packaging tools
   - Fixes code formatting with `dotnet format`
   - Builds the .NET application
   - Tests the CLI functionality
   - Creates Debian package
   - Uploads package as artifact

3. **Deploy Job** (runs on `docker-build` runner):
   - Downloads package artifact
   - Sets up SSH with APT repository server
   - Deploys package to APT repository
   - Verifies deployment
   - Sends notification

#### Key Configuration Details

**Architecture**: Package uses `amd64` architecture (not `all`) to match repository structure
**Repository**: Deployed to `http://tools.apt.home.net`
**Package Name**: `sns` (not `sins-cli`)
**Dependencies**: Requires .NET runtime 9.0, 8.0, or 7.0

### Deployment Process

#### APT Repository Structure
```
http://tools.apt.home.net/
├── Packages              # Main package index
├── Packages.gz           # Compressed package index
├── Release               # Repository metadata
├── amd64/                # amd64 architecture packages
│   └── sns_1.0.0_amd64.deb
├── i386/                 # i386 architecture packages
└── source/               # Source packages
```

#### Deployment Steps
1. **Package Creation**: Build Debian package with correct architecture
2. **SSH Transfer**: Copy package to repository server (`10.11.2.10`)
3. **Repository Update**: Run `add-package.sh` script on server
4. **Index Generation**: Update Packages index and metadata
5. **Verification**: Check package availability in repository

#### SSH Configuration
- **Server**: `10.11.2.10` (APT repository server)
- **User**: `jaal`
- **Key**: Stored in GitHub Environment secret `APT_REPO_SSH_KEY`
- **Script**: `/usr/local/bin/add-package.sh` on repository server

### Usage

#### Installation
```bash
# Add repository (if not already added)
echo "deb http://tools.apt.home.net /" | sudo tee /etc/apt/sources.list.d/custom.list

# Update package list
sudo apt update

# Install CLI
sudo apt install sns
```

#### Basic Usage
```bash
# Show help
sns --help

# Set server and token
sns --server http://dns-server:8080 --token your-jwt-token

# Authentication
sns auth login username password

# DNS management
sns dns list
sns dns add example.com A 192.168.1.100
sns dns delete example.com A

# Cache management
sns cache list
sns cache clear

# System management
sns system health
sns system stats
```

### Troubleshooting

#### Common Build Issues

**Code Formatting Errors**
```bash
# Fix formatting before build
dotnet format --verbosity quiet
```

**Architecture Mismatch**
- Ensure package uses `amd64` architecture (not `all`)
- Check `debian/control` and `build-package.sh`

**Missing Dependencies**
```bash
# Install required tools
sudo apt install dpkg-dev build-essential fakeroot
```

#### Deployment Issues

**SSH Authentication**
- Verify `APT_REPO_SSH_KEY` secret is set in GitHub Environment
- Check SSH key permissions and format
- Ensure repository server is accessible from runner

**Package Not Found After Deployment**
```bash
# Check repository structure
curl -s http://tools.apt.home.net/Packages | grep sns

# Verify package location
curl -s http://tools.apt.home.net/amd64/ | grep sns
```

**Repository URL Mismatch**
- Package deploys to `http://custom-repo.home.net:8080`
- Should be accessible via `http://tools.apt.home.net`
- Check HAProxy configuration if URL doesn't work

#### Version Management

**Creating New Release**
```bash
# Create and push tag
git tag cli-v1.0.0
git push origin cli-v1.0.0

# This triggers the CI/CD pipeline automatically
```

**Version Extraction**
- Version is extracted from git tag (removes `cli-v` prefix)
- Example: tag `cli-v1.0.0` becomes version `1.0.0`

### Security Considerations

#### Package Security
- Packages are signed and verified
- Repository uses HTTPS
- SSH keys are stored securely in GitHub secrets

#### API Security
- CLI uses JWT authentication
- Tokens can be provided via command line or environment variable
- API calls use HTTPS (when configured)

### Performance Optimization

#### Build Performance
- Uses self-hosted runners for faster builds
- Parallel job execution
- Cached dependencies

#### Runtime Performance
- Minimal dependencies
- Efficient JSON serialization
- Connection pooling for HTTP requests

---

## Future Roadmap

### Planned Enhancements

#### Advanced DNS Features
- **DNSSEC (authoritative)**: Implemented — see [docs/dnssec.md](docs/dnssec.md)
- **DNS over HTTPS**: DoH support (planned)
- **DNS over TLS**: DoT support
- **GeoDNS**: Geographic DNS routing

#### Management Features
- **Zone Management**: DNS zone management
- **Bulk Operations**: Bulk record operations
- **API Rate Limiting**: Advanced rate limiting
- **Webhook Integration**: External system integration

#### Monitoring Features
- **Advanced Analytics**: Detailed analytics and reporting
- **Custom Dashboards**: User-defined dashboards
- **Alert Management**: Advanced alerting system
- **Performance Optimization**: Automated performance tuning

### Scalability Roadmap

#### Short Term (Next 3 months)
- **Performance Optimization**: Query optimization and caching
- **Monitoring Enhancement**: Advanced monitoring capabilities
- **Security Hardening**: Additional security features

#### Medium Term (3-6 months)
- **Microservices Architecture**: Service decomposition
- **Kubernetes Deployment**: Cloud-native deployment
- **Multi-region Support**: Geographic distribution

#### Long Term (6+ months)
- **AI/ML Integration**: Intelligent DNS management
- **Global Distribution**: Multi-region deployment
- **Advanced Analytics**: Machine learning insights

### Technical Debt

#### Code Quality
- **Unit Testing**: Increase test coverage
- **Integration Testing**: End-to-end testing
- **Code Documentation**: API documentation
- **Performance Testing**: Load testing

#### Infrastructure
- **Monitoring**: Prometheus/Grafana integration
- **Logging**: Centralized logging (ELK stack)
- **Backup**: Automated backup procedures
- **Disaster Recovery**: Recovery procedures

---

## Conclusion

SiNS represents a modern approach to DNS server management, combining traditional DNS functionality with contemporary web technologies. The project demonstrates:

- **Containerization**: Modern deployment with Docker
- **CI/CD Automation**: Automated testing and deployment
- **Security**: JWT authentication and HTTPS
- **Scalability**: Designed for horizontal scaling
- **Maintainability**: Clear separation of concerns

The architecture supports both development and production environments, with comprehensive monitoring and troubleshooting capabilities. The project is well-positioned for future enhancements and scaling requirements.

---

*Last Updated: August 2025*
*Version: 1.0.0*
*Documentation Version: 1.0*
