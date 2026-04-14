# Docker Hub Overview

This document provides a comprehensive overview of SiNS Docker Hub integration, available images, and usage instructions.

## Table of Contents

- [Overview](#overview)
- [Available Images](#available-images)
- [Image Tags](#image-tags)
- [Image Details](#image-details)
- [Usage Examples](#usage-examples)
- [CI/CD Integration](#cicd-integration)
- [Security](#security)
- [Troubleshooting](#troubleshooting)

## Overview

SiNS (Simple Name Server) is available as pre-built Docker images on Docker Hub, providing a streamlined deployment experience for production environments. The images are automatically built and published through GitHub Actions CI/CD pipelines.

### Docker Hub Repository
- **Repository**: `swipentap/sins`
- **URL**: https://hub.docker.com/r/swipentap/sins
- **Source**: https://github.com/swipentap/SiNS

## Available Images

### Main Application Image
- **Image**: `swipentap/sins`
- **Description**: Complete SiNS DNS server with web management interface
- **Base**: .NET 8.0 runtime
- **Architecture**: linux/amd64
- **Size**: ~200MB

### Image Features
- **DNS Server**: Authoritative and recursive DNS functionality
- **Web Interface**: Vue.js management interface
- **Database**: PostgreSQL integration
- **Authentication**: JWT-based authentication
- **Caching**: Intelligent DNS caching system
- **Health Checks**: Built-in health monitoring
- **Production Ready**: Optimized for production deployment

## Image Tags

### Latest Tags
- `swipentap/sins:latest` - Latest stable version
- `swipentap/sins:main` - Latest development version

### Version Tags
- `swipentap/sins:1.0.6` - Specific version (current)
- `swipentap/sins:1.0.5` - Previous version
- `swipentap/sins:1.0.4` - Previous version
- `swipentap/sins:1.0.3` - Previous version
- `swipentap/sins:1.0.2` - Previous version
- `swipentap/sins:1.0.1` - Previous version
- `swipentap/sins:1.0.0` - Initial release

### Semantic Version Tags
- `swipentap/sins:1.0` - Latest 1.0.x version
- `swipentap/sins:1` - Latest 1.x.x version

### Development Tags
- `swipentap/sins:main-sha-<commit>` - Development builds from main branch
- `swipentap/sins:<branch>-sha-<commit>` - Branch-specific development builds

## Image Details

### Base Image
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
```

### Runtime Environment
- **.NET Version**: 8.0
- **OS**: Debian-based Linux
- **Architecture**: x86_64 (amd64)
- **Shell**: bash
- **User**: Non-root (UID 1000)

### Exposed Ports
- **80**: HTTP (Web interface)
- **53**: DNS (UDP/TCP)

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Production/Development
- `ASPNETCORE_URLS`: HTTP binding (default: http://+:80)
- `ConnectionStrings__DefaultConnection`: Database connection string

### Volumes
- `/app/wwwroot`: Web interface static files
- `/app/logs`: Application logs (optional)

### Health Check
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost/api/health || exit 1
```

## Usage Examples

### Basic Usage

#### Pull and Run
```bash
# Pull latest image
docker pull swipentap/sins:latest

# Run with basic configuration
docker run -d \
  --name sins-dns \
  -p 80:80 \
  -p 53:53/udp \
  -p 53:53/tcp \
  --cap-add=NET_BIND_SERVICE \
  --user root \
  swipentap/sins:latest
```

#### Using Docker Compose
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: dns_server
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      dns-network:
        ipv4_address: 172.20.0.2

  dns-server:
    image: swipentap/sins:latest
    depends_on:
      - postgres
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=172.20.0.2;Database=dns_server;Username=postgres;Password=postgres
    ports:
      - "80:80"
      - "53:53/udp"
      - "53:53/tcp"
    networks:
      dns-network:
        ipv4_address: 172.20.0.3
    cap_add:
      - NET_BIND_SERVICE
    user: "0"
    restart: unless-stopped

volumes:
  postgres_data:

networks:
  dns-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

### Production Deployment

#### Automated Deployment
```bash
# Create deployment directory
mkdir sins-production && cd sins-production

# Download configuration files
curl -O https://raw.githubusercontent.com/swipentap/SiNS/main/docker-compose.yml
curl -O https://raw.githubusercontent.com/swipentap/SiNS/main/deploy.sh
chmod +x deploy.sh

# Deploy (requires root)
sudo ./deploy.sh
```

#### Manual Deployment
```bash
# Stop system DNS services
sudo systemctl stop systemd-resolved
sudo systemctl disable systemd-resolved

# Configure network DNS
echo "nameserver 127.0.0.1" | sudo tee /etc/resolv.conf
echo "nameserver 8.8.8.8" | sudo tee -a /etc/resolv.conf
sudo chattr +i /etc/resolv.conf

# Start services
docker-compose up -d
```

### Development Usage

#### Local Development
```bash
# Pull development image
docker pull swipentap/sins:main

# Run with development configuration
docker run -d \
  --name sins-dev \
  -p 8080:80 \
  -p 5354:53/udp \
  -p 5354:53/tcp \
  -e ASPNETCORE_ENVIRONMENT=Development \
  swipentap/sins:main
```

#### Testing Specific Versions
```bash
# Test specific version
docker run -d \
  --name sins-test \
  -p 8081:80 \
  -p 5355:53/udp \
  swipentap/sins:1.0.6
```

## CI/CD Integration

### GitHub Actions Workflow

The Docker images are automatically built and published through GitHub Actions:

#### Build and Release Workflow
- **Trigger**: Tag pushes (e.g., `v1.0.6`)
- **Actions**:
  1. Build Docker image
  2. Run security scans (Trivy)
  3. Push to Docker Hub
  4. Create GitHub release

#### CI Workflow
- **Trigger**: Push to main branch
- **Actions**:
  1. Run tests
  2. Build Docker image
  3. Push to Docker Hub with `latest` tag

### Image Building Process

```yaml
# .github/workflows/build-and-release.yml
- name: Build and push Docker image
  uses: docker/build-push-action@v5
  with:
    context: .
    push: true
    tags: ${{ steps.meta.outputs.tags }}
    labels: ${{ steps.meta.outputs.labels }}
    cache-from: type=gha
    cache-to: type=gha,mode=max
```

### Tag Strategy
- **Semantic Versioning**: `1.0.6`, `1.0`, `latest`
- **Branch Tags**: `main`, `main-sha-<commit>`
- **Development**: `main-sha-<commit>` for development builds

## Security

### Image Security Features
- **Non-root User**: Application runs as non-root user (UID 1000)
- **Minimal Base**: Uses official .NET 8.0 runtime image
- **Security Scans**: Automated Trivy vulnerability scanning
- **Regular Updates**: Base images updated regularly

### Security Best Practices

#### Production Deployment
```bash
# Use specific version tags (not latest)
docker run swipentap/sins:1.0.6

# Run with read-only root filesystem
docker run --read-only swipentap/sins:latest

# Use secrets for sensitive data
docker run -e ConnectionStrings__DefaultConnection="$DB_CONNECTION" swipentap/sins:latest
```

#### Network Security
```bash
# Use custom networks
docker network create sins-network
docker run --network sins-network swipentap/sins:latest

# Limit port exposure
docker run -p 127.0.0.1:80:80 swipentap/sins:latest
```

### Vulnerability Scanning
```bash
# Scan image for vulnerabilities
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image swipentap/sins:latest

# Scan with specific severity
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image --severity HIGH,CRITICAL swipentap/sins:latest
```

## Troubleshooting

### Common Issues

#### Image Pull Issues
```bash
# Check Docker Hub connectivity
docker pull hello-world

# Pull with verbose output
docker pull --progress=plain swipentap/sins:latest

# Check available tags
curl -s https://registry.hub.docker.com/v2/repositories/swipentap/sins/tags/ | jq '.results[].name'
```

#### Port Binding Issues
```bash
# Check if port 53 is in use
sudo lsof -i :53

# Use alternative ports for testing
docker run -p 5354:53/udp -p 5354:53/tcp swipentap/sins:latest

# Check container logs
docker logs <container-name>
```

#### Permission Issues
```bash
# Run with proper capabilities
docker run --cap-add=NET_BIND_SERVICE --user root swipentap/sins:latest

# Check container user
docker exec <container-name> whoami
```

### Debugging Commands

#### Container Inspection
```bash
# Inspect container
docker inspect <container-name>

# Check container processes
docker exec <container-name> ps aux

# Check container network
docker exec <container-name> ip addr show

# Check container logs
docker logs -f <container-name>
```

#### Health Check
```bash
# Test health endpoint
curl http://localhost/api/health

# Check health status
docker inspect <container-name> | jq '.[0].State.Health'

# Manual health check
docker exec <container-name> curl -f http://localhost/api/health
```

### Performance Monitoring

#### Resource Usage
```bash
# Monitor container resources
docker stats <container-name>

# Check memory usage
docker exec <container-name> free -h

# Check disk usage
docker exec <container-name> df -h
```

#### DNS Performance
```bash
# Test DNS response time
time dig @127.0.0.1 google.com

# Benchmark DNS queries
for i in {1..100}; do dig @127.0.0.1 google.com > /dev/null; done

# Check DNS cache
curl http://localhost/api/dns/cache
```

## Support and Resources

### Documentation
- **Main Documentation**: https://github.com/swipentap/SiNS
- **Installation Guide**: [docs/installation.md](installation.md)
- **Quick Start**: [docs/quick-start.md](quick-start.md)
- **API Reference**: [docs/api-reference.md](api-reference.md)

### Community
- **GitHub Issues**: https://github.com/swipentap/SiNS/issues
- **GitHub Discussions**: https://github.com/swipentap/SiNS/discussions
- **Docker Hub**: https://hub.docker.com/r/swipentap/sins

### Version History
- **Changelog**: Check GitHub releases for detailed version history
- **Migration Guide**: Review breaking changes between major versions
- **Deprecation Policy**: Check documentation for deprecated features

---

**Note**: This document is maintained as part of the SiNS project. For the latest information, always refer to the official documentation and Docker Hub repository.
