# Quick Start Guide

Get your SiNS (Simple Name Server) up and running in minutes with this quick start guide.

## Prerequisites

Before you begin, ensure you have:

- **Linux system** (Ubuntu 20.04+, Debian 11+, CentOS 8+)
- **Root access** (for port 53)
- **Docker and Docker Compose** installed
- **Internet connection** for downloading images

## Step 1: Prepare Deployment

### Option A: Using Docker Hub (Recommended)

```bash
# Create deployment directory
mkdir sins-production && cd sins-production

# Download configuration files
curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/docker-compose.yml
curl -O https://raw.githubusercontent.com/judyandiealvarez/SiNS/main/deploy.sh
chmod +x deploy.sh
```

### Option B: Clone Repository

```bash
git clone https://github.com/judyandiealvarez/SiNS.git
cd sins
```

## Step 2: Deploy SiNS

Run the automated deployment script:

```bash
sudo ./deploy.sh
```

This script will:
- Stop system DNS services
- Configure network DNS settings
- Deploy SiNS with static IPs using Docker Hub image
- Test the deployment
- Show status information

## Step 3: Access the Web Interface

1. **Open your browser**
2. **Navigate to**: `http://localhost`
3. **Login** with default credentials:
   - Username: `admin`
   - Password: `admin123`

## Step 4: Test DNS Resolution

Test that your SiNS DNS server is working:

```bash
# Test DNS resolution
dig @127.0.0.1 google.com

# Test with nslookup
nslookup google.com 127.0.0.1
```

## Step 5: Add Your First DNS Record

1. **Login to the web interface**
2. **Navigate to "DNS Records"**
3. **Click "Add Record"**
4. **Fill in the details**:
   - Name: `example.com`
   - Type: `A`
   - Value: `192.168.1.100`
   - TTL: `3600`
5. **Click "Add Record"**

## Step 6: Test Your DNS Record

```bash
# Test your new record
dig @127.0.0.1 example.com

# Should return: 192.168.1.100
```

## Step 7: Configure Your System DNS

Set your system to use SiNS:

```bash
# Backup current DNS settings
sudo cp /etc/resolv.conf /etc/resolv.conf.backup

# Set localhost as primary DNS
echo "nameserver 127.0.0.1" | sudo tee /etc/resolv.conf
echo "nameserver 8.8.8.8" | sudo tee -a /etc/resolv.conf

# Prevent automatic changes
sudo chattr +i /etc/resolv.conf
```

## Step 8: Verify Everything Works

Test that your system is using SiNS:

```bash
# Test system DNS resolution
nslookup google.com

# Should show: Server: 127.0.0.1
```

## What's Next?

### Basic Configuration

1. **Change default password**:
   - Go to User Management
   - Edit the admin user
   - Set a strong password

2. **Configure cache settings**:
   - Go to Settings
   - Adjust cache timeout (default: 60 minutes)
   - Set upstream DNS servers

3. **Add more DNS records**:
   - Add A records for your domains
   - Add CNAME records for subdomains
   - Add MX records for email

### Advanced Configuration

1. **Security hardening**:
   - Update JWT key in `sins/appsettings.json`
   - Configure firewall rules
   - Set up SSL/TLS certificates

2. **Performance tuning**:
   - Monitor cache hit rates
   - Adjust cache timeout based on usage
   - Configure resource limits

3. **Monitoring**:
   - Check service health regularly
   - Monitor DNS query statistics
   - Set up log monitoring

## Troubleshooting

### Common Issues

#### DNS Server Not Starting
```bash
# Check container status
docker-compose ps

# View logs
docker-compose logs dns-server
```

#### Port 53 Already in Use
```bash
# Check what's using port 53
sudo netstat -tulpn | grep :53

# Stop conflicting services
sudo systemctl stop systemd-resolved
```

#### Web Interface Not Accessible
```bash
# Check if containers are running
docker-compose ps

# Test web interface
curl http://localhost/api/dns/health
```

#### DNS Resolution Not Working
```bash
# Test DNS server directly
dig @127.0.0.1 google.com

# Check DNS server logs
docker-compose logs dns-server
```

### Getting Help

1. **Check the logs**: `docker-compose logs`
2. **Review documentation**: See the full documentation
3. **Test step by step**: Follow the troubleshooting guide
4. **Community support**: Check project issues

## Quick Commands Reference

### Service Management
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# Restart services
docker-compose restart

# View logs
docker-compose logs -f
```

### DNS Testing
```bash
# Test DNS resolution
dig @127.0.0.1 google.com

# Test with nslookup
nslookup google.com 127.0.0.1

# Test specific record type
dig @127.0.0.1 google.com AAAA
```

### System DNS Configuration
```bash
# Check current DNS
cat /etc/resolv.conf

# Set DNS server
echo "nameserver 127.0.0.1" | sudo tee /etc/resolv.conf

# Restore original DNS
sudo cp /etc/resolv.conf.backup /etc/resolv.conf
```

### Database Access
```bash
# Connect to database
docker exec -it dns-postgres psql -U postgres -d dns_server

# Backup database
docker exec dns-postgres pg_dump -U postgres dns_server > backup.sql

# Restore database
docker exec -i dns-postgres psql -U postgres dns_server < backup.sql
```

## Performance Tips

### Optimize Cache Performance
- **Monitor cache hit rate** in the web interface
- **Adjust cache timeout** based on your needs
- **Clear expired cache** regularly

### System Optimization
- **Use SSD storage** for better database performance
- **Allocate sufficient memory** (4GB+ recommended)
- **Monitor resource usage** with `docker stats`

### Network Optimization
- **Use reliable upstream DNS servers**
- **Configure firewall rules** properly
- **Monitor network latency**

## Security Checklist

### Immediate Actions
- [ ] Change default admin password
- [ ] Update JWT key in configuration
- [ ] Configure firewall rules
- [ ] Set up SSL/TLS certificates

### Ongoing Security
- [ ] Regular security updates
- [ ] Monitor access logs
- [ ] Backup configuration and data
- [ ] Review user permissions

### Advanced Security
- [ ] Set up intrusion detection
- [ ] Configure log monitoring
- [ ] Implement rate limiting
- [ ] Set up alerting

## Next Steps

Now that you have your DNS server running:

1. **Read the full documentation** for detailed information
2. **Configure your domains** with appropriate DNS records
3. **Set up monitoring** to track performance and health
4. **Plan for backup and recovery** procedures
5. **Consider scaling** as your needs grow

For detailed information on any topic, see the complete documentation:

- [Installation Guide](installation.md)
- [Web Interface Guide](web-interface.md)
- [API Reference](api-reference.md)
- [Architecture Documentation](architecture.md)
- [Production Deployment](production-deployment.md)

## Support

If you encounter issues:

1. **Check the troubleshooting section** in this guide
2. **Review the logs**: `docker-compose logs`
3. **Test step by step** to isolate the problem
4. **Check the full documentation** for detailed guides
5. **Community support**: Check project issues and discussions

Your SiNS (Simple Name Server) is now ready to serve your network! 🚀
