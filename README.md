# SiNS - Simple Name Server

[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](http://makeapullrequest.com)

**SiNS** stands for **[Si]mple [N]ame [S]erver** - a complete DNS server solution with web-based management interface, built with .NET 8, PostgreSQL, and Vue.js.

## Features

- **SiNS DNS Server**: Authoritative and recursive DNS server supporting UDP and TCP
- **Web Management**: Modern Vue.js interface with Vuex state management
- **Database Storage**: PostgreSQL for DNS records, cache, and configuration
- **Authentication**: JWT-based authentication with role-based access
- **Caching**: Intelligent DNS caching with configurable TTL
- **Real-time Configuration**: Database-driven configuration with immediate effect
- **Production Ready**: Static IP addressing and proper service management

## Architecture

### Services
- **SiNS DNS Server**: .NET 8 application handling DNS queries (IP: 172.20.0.3)
- **PostgreSQL**: Database for DNS records and configuration (IP: 172.20.0.2)
- **Web Interface**: Vue.js application for management (Port 80)

### Network Configuration
- **Subnet**: 172.20.0.0/16
- **Gateway**: 172.20.0.1
- **SiNS DNS Server**: 172.20.0.3
- **PostgreSQL**: 172.20.0.2

### Production Configuration
- **DNS Port**: 53 (standard DNS port)
- **Web Interface**: http://localhost
- **Static IPs**: 172.20.0.2 (PostgreSQL), 172.20.0.3 (SiNS DNS Server)
- **Network**: 172.20.0.0/16 subnet with static addressing
- **Health Checks**: Automatic service monitoring and restart

## Production Deployment

### Prerequisites
- Linux system (tested on Ubuntu/Debian)
- Docker and Docker Compose
- Root access (required for port 53 and system DNS management)

### Quick Deployment

**⚠️ Important**: This will stop your system DNS resolver and replace it with our DNS server.

```bash
# Clone the repository
git clone https://github.com/judyandiealvarez/SiNS.git
cd sins

# Run deployment script (requires root)
sudo ./deploy.sh
```

### Using Docker Hub Image (Recommended)

For production deployment, you can use the pre-built Docker Hub image:

```bash
# Create deployment directory
mkdir sins-production && cd sins-production

# Download docker-compose.yml
curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/docker-compose.yml

# Download deployment script
curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/deploy.sh
chmod +x deploy.sh

# Run deployment (requires root)
sudo ./deploy.sh
```

The deployment script will:
1. Stop system DNS services (systemd-resolved, bind9, etc.)
2. Configure network DNS settings
3. Deploy the DNS server with static IP addresses
4. Test the deployment
5. Show status information

## Configuration

### DNS Server Settings
- **Cache Timeout**: Configurable in minutes (default: 60)
- **Upstream Servers**: Fallback DNS servers (default: 8.8.8.8, 1.1.1.1)
- **Ports**: UDP/TCP port 53 (configurable)

### Database Configuration
- **Host**: 172.20.0.2 (PostgreSQL static IP)
- **Database**: dns_server
- **Username**: postgres
- **Password**: postgres

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration (Admin only)
- `GET /api/auth/users` - List users (Admin only)

### DNS Management
- `GET /api/dns/records` - List DNS records
- `POST /api/dns/records` - Create DNS record
- `PUT /api/dns/records/{id}` - Update DNS record
- `DELETE /api/dns/records/{id}` - Delete DNS record

### Cache Management
- `GET /api/dns/cache` - List cache entries
- `DELETE /api/dns/cache` - Clear all cache
- `DELETE /api/dns/cache/expired` - Clear expired cache

### Configuration
- `GET /api/dns/config` - Get server configuration
- `POST /api/dns/config` - Update server configuration

### Health Check
- `GET /api/dns/health` - Service health check

## Web Interface

### Features
- **Dashboard**: Overview of DNS records, cache, and users
- **DNS Records**: Full CRUD operations for DNS records
- **Cache Management**: View and manage DNS cache
- **User Management**: Add and manage users
- **Settings**: Configure server parameters

### Authentication
- **Default Admin**: admin / admin123
- **Role-based Access**: Admin and User roles
- **JWT Tokens**: Secure authentication

## Production Features

### System DNS Management
The deployment script automatically stops common system DNS services:
- systemd-resolved (Ubuntu/Debian)
- bind9 (BIND DNS server)
- dnsmasq (Lightweight DNS server)

This allows SiNS to take over as the primary DNS server on the system.

### Static Network Configuration
- **Subnet**: 172.20.0.0/16 with static IP addressing
- **PostgreSQL**: 172.20.0.2
- **SiNS DNS Server**: 172.20.0.3
- **Port 53**: Freed from system services for SiNS DNS server use

### Health Monitoring
- PostgreSQL health check using `pg_isready`
- SiNS DNS server health check using HTTP endpoint
- Automatic service restart on failure
- Service dependency management

### Security Features
- JWT authentication with configurable keys
- Role-based access control (Admin/User roles)
- Database connection with static IP addressing
- Container isolation with proper networking
- Privileged container for port 53 binding

## Troubleshooting

### DNS Resolution Issues
```bash
# Test DNS resolution
nslookup google.com 127.0.0.1
dig @127.0.0.1 google.com

# Check container status
docker-compose ps

# View logs
docker-compose logs dns-server
```

### Web Interface Issues
```bash
# Check web interface
curl http://localhost

# Check container health
docker-compose ps
```

### Database Issues
```bash
# Check PostgreSQL
docker-compose logs postgres

# Connect to database
docker exec -it dns-postgres psql -U postgres -d dns_server
```

### Restore System DNS
If you need to restore the original system DNS:
```bash
# Stop DNS server
docker-compose down

# Restore system DNS
sudo systemctl enable systemd-resolved
sudo systemctl start systemd-resolved

# Restore resolv.conf
sudo chattr -i /etc/resolv.conf
sudo cp /etc/resolv.conf.backup /etc/resolv.conf
```

## Development

### Building
```bash
# Build Docker image
docker-compose build
```

### Testing
```bash
# Test DNS functionality
dig @localhost google.com
nslookup google.com localhost

# Test web interface
curl http://localhost/api/dns/health
```

## License

This project is licensed under the GNU Lesser General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details on how to submit pull requests, report issues, and contribute to the project.

### Quick Start for Contributors

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request
