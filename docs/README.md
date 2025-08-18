# DNS Server Documentation

Welcome to the DNS Server documentation. This is a complete DNS server solution with web-based management interface, built with .NET 8, PostgreSQL, and Vue.js.

## 📚 Documentation Index

### Getting Started
- [Installation Guide](installation.md) - Complete setup instructions
- [Quick Start](quick-start.md) - Get up and running in minutes
- [Configuration](configuration.md) - Server configuration options

### User Guides
- [Web Interface Guide](web-interface.md) - Using the management UI
- [DNS Management](dns-management.md) - Managing DNS records and zones
- [User Management](user-management.md) - Managing users and permissions
- [Cache Management](cache-management.md) - Understanding and managing DNS cache

### Administration
- [Production Deployment](production-deployment.md) - Production deployment guide
- [Monitoring & Logging](monitoring.md) - Monitoring and troubleshooting
- [Security Guide](security.md) - Security best practices
- [Backup & Recovery](backup-recovery.md) - Data backup and disaster recovery

### Technical Reference
- [API Reference](api-reference.md) - Complete API documentation
- [Architecture](architecture.md) - System architecture and design
- [Database Schema](database-schema.md) - Database structure and relationships
- [Network Configuration](network-config.md) - Network setup and static IPs

### Development
- [Development Setup](development.md) - Setting up development environment
- [Contributing](contributing.md) - Contributing to the project
- [Testing](testing.md) - Testing procedures and guidelines

## 🚀 Quick Overview

### Features
- **Hybrid DNS Server**: Authoritative and recursive DNS server
- **Web Management**: Modern Vue.js interface with Vuex state management
- **Database Storage**: PostgreSQL for DNS records, cache, and configuration
- **Authentication**: JWT-based authentication with role-based access
- **Caching**: Intelligent DNS caching with configurable TTL
- **Real-time Configuration**: Database-driven configuration with immediate effect
- **Production Ready**: Static IP addressing and proper service management

### Architecture
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   DNS Client    │    │   Web Browser   │    │  Upstream DNS   │
│                 │    │                 │    │    Servers      │
└─────────┬───────┘    └─────────┬───────┘    └─────────┬───────┘
          │                      │                      │
          │ UDP/TCP:53           │ HTTP:80              │ UDP:53
          │                      │                      │
    ┌─────▼──────────────────────▼──────────────────────▼─────┐
    │                    DNS Server                           │
    │  ┌─────────────────┐  ┌─────────────────┐              │
    │  │   DNS Engine    │  │   Web API       │              │
    │  │                 │  │                 │              │
    │  │ • UDP/TCP       │  │ • Authentication│              │
    │  │ • Caching       │  │ • Record Mgmt   │              │
    │  │ • Recursion     │  │ • Statistics    │              │
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

### Network Configuration
- **Subnet**: 172.20.0.0/16
- **PostgreSQL**: 172.20.0.2
- **DNS Server**: 172.20.0.3
- **Ports**: 53 (DNS), 80 (Web)

## 📋 Prerequisites

### Production Requirements
- Linux system (Ubuntu 20.04+ / Debian 11+ / CentOS 8+)
- Docker and Docker Compose
- Root access (for port 53)
- 2GB RAM minimum, 4GB recommended
- 10GB disk space

### Development Requirements
- Docker Desktop (macOS/Windows) or Docker on Linux
- Git
- Text editor or IDE

## 🔧 Quick Installation

### Production Deployment
```bash
# Clone repository
git clone <repository-url>
cd sins

# Run deployment script
sudo ./deploy.sh
```

### Development Setup
```bash
# Clone repository
git clone <repository-url>
cd sins

# Start services
docker-compose up -d

# Access web interface
open http://localhost
# Login: admin / admin123
```

## 🎯 Common Use Cases

### Small Business DNS
- Manage internal DNS records
- Cache external DNS queries
- Web-based management interface
- User access control

### Development Environment
- Local DNS server for development
- Custom domain resolution
- Cache frequently accessed domains
- Easy record management

### Network Infrastructure
- Primary DNS server for network
- Authoritative DNS for custom domains
- Recursive DNS with caching
- Monitoring and statistics

## 📊 System Requirements

### Minimum Requirements
- **CPU**: 1 core
- **RAM**: 2GB
- **Storage**: 10GB
- **Network**: 100Mbps

### Recommended Requirements
- **CPU**: 2+ cores
- **RAM**: 4GB+
- **Storage**: 20GB+ SSD
- **Network**: 1Gbps

### Performance Considerations
- DNS queries: 1000+ queries/second
- Cache size: Configurable, default 1000 records
- Database connections: 10-50 concurrent
- Web interface: 10+ concurrent users

## 🔒 Security Features

- **Authentication**: JWT-based with role-based access
- **Network Isolation**: Docker containers with static IPs
- **Database Security**: PostgreSQL with connection encryption
- **API Security**: HTTPS-ready, input validation
- **Access Control**: Admin and User roles

## 📈 Monitoring & Metrics

- **DNS Statistics**: Query counts, cache hit rates
- **System Health**: Service status, resource usage
- **Performance Metrics**: Response times, throughput
- **Logging**: Structured logging with different levels
- **Alerts**: Health check failures, service restarts

## 🆘 Support

### Getting Help
1. Check the [Troubleshooting Guide](troubleshooting.md)
2. Review [Common Issues](common-issues.md)
3. Check the logs: `docker-compose logs`
4. Test DNS resolution: `dig @127.0.0.1 google.com`

### Community
- GitHub Issues: Report bugs and request features
- Documentation: Comprehensive guides and references
- Examples: Sample configurations and use cases

## 📄 License

This project is licensed under the MIT License. See [LICENSE](../LICENSE) for details.

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](contributing.md) for details on how to submit pull requests, report issues, and contribute to the project.

---

**Next Steps**: Start with the [Installation Guide](installation.md) or [Quick Start](quick-start.md) to get up and running quickly.
