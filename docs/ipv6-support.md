# IPv6 Support in SiNS DNS Server

SiNS (Simple Name Server) provides comprehensive IPv6 support for modern network environments.

## 🌐 IPv6 Features

### **Dual-Stack DNS Server**
- **IPv4 + IPv6**: Simultaneous support for both protocols
- **Dual-Stack Binding**: Listens on both IPv4 and IPv6 interfaces
- **Automatic Protocol Selection**: Clients can connect via either protocol

### **AAAA Record Support**
- **Full AAAA Support**: Complete IPv6 address record handling
- **IPv6 Response Parsing**: Extracts IPv6 addresses from DNS responses
- **IPv6 Caching**: Caches AAAA records with proper TTL

### **IPv6 Upstream Servers**
- **Google DNS IPv6**: `2001:4860:4860::8888`
- **Cloudflare DNS IPv6**: `2606:4700:4700::1111`
- **Fallback Mechanism**: Automatic fallback between IPv4 and IPv6 upstreams

## 🚀 IPv6 Configuration

### **Docker Compose IPv6 Setup**

```yaml
networks:
  dns-network:
    driver: bridge
    enable_ipv6: true
    ipam:
      config:
        - subnet: 172.20.0.0/16
          gateway: 172.20.0.1
        - subnet: 2001:db8::/64
          gateway: 2001:db8::1

services:
  postgres:
    networks:
      dns-network:
        ipv4_address: 172.20.0.2
        ipv6_address: 2001:db8::2

  dns-server:
    networks:
      dns-network:
        ipv4_address: 172.20.0.3
        ipv6_address: 2001:db8::3
```

### **Default Upstream Servers**

```json
{
  "upstreamServers": [
    "8.8.8.8",           // Google DNS IPv4
    "1.1.1.1",           // Cloudflare DNS IPv4
    "2001:4860:4860::8888", // Google DNS IPv6
    "2606:4700:4700::1111"  // Cloudflare DNS IPv6
  ]
}
```

## 🔧 IPv6 Implementation Details

### **Network Binding**
```csharp
// Dual-stack UDP binding
_udpClient.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
_udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, _udpPort));

// Dual-stack TCP listener
_tcpListener = new TcpListener(IPAddress.IPv6Any, _tcpPort);
```

### **IPv6 Address Parsing**
```csharp
// Handle both IPv4 and IPv6 addresses
IPAddress ipAddress;
if (!IPAddress.TryParse(server, out ipAddress))
{
    _logger.LogWarning("Invalid IP address format: {Server}", server);
    return null;
}
```

### **AAAA Record Processing**
```csharp
// Extract IPv6 address from last 16 bytes
else if (type.ToUpper() == "AAAA" && response.Length >= 16)
{
    var last16Bytes = response.Skip(response.Length - 16).Take(16).ToArray();
    var ipv6 = BitConverter.ToString(last16Bytes).Replace("-", ":");
    ips.Add(ipv6);
}
```

## 🧪 IPv6 Testing

### **Test IPv6 DNS Resolution**

```bash
# Test AAAA record resolution
dig AAAA google.com @localhost

# Test IPv6 connectivity
dig AAAA google.com @2001:db8::3

# Test dual-stack fallback
dig google.com @localhost
```

### **Test IPv6 Upstream Servers**

```bash
# Test IPv6 upstream directly
dig google.com @2001:4860:4860::8888

# Test IPv6 upstream via SiNS
dig google.com @localhost
```

## 📊 IPv6 Monitoring

### **Web UI IPv6 Support**
- **Settings Page**: Configure IPv6 upstream servers
- **Cache View**: Display IPv6 addresses in cache
- **Statistics**: Monitor IPv6 query performance

### **Logging IPv6 Activity**
```csharp
_logger.LogInformation("IPv6 query from {RemoteEndPoint}", remoteEndPoint);
_logger.LogInformation("IPv6 upstream response from {Server}", upstreamServer);
```

## 🌍 IPv6 Deployment Considerations

### **Production IPv6 Setup**

1. **Enable IPv6 on Host**
   ```bash
   # Enable IPv6 on Linux
   echo 'net.ipv6.conf.all.disable_ipv6 = 0' >> /etc/sysctl.conf
   sysctl -p
   ```

2. **Configure IPv6 Network**
   ```bash
   # Assign IPv6 address to interface
   ip addr add 2001:db8::1/64 dev eth0
   ```

3. **Firewall Rules**
   ```bash
   # Allow IPv6 DNS traffic
   ufw allow 53/tcp
   ufw allow 53/udp
   ```

### **IPv6 Security**
- **Dual-Stack Security**: Both IPv4 and IPv6 security policies
- **IPv6 Firewall**: Configure IPv6-specific firewall rules
- **IPv6 Monitoring**: Monitor IPv6 traffic patterns

## 🔍 IPv6 Troubleshooting

### **Common IPv6 Issues**

1. **IPv6 Not Enabled**
   ```bash
   # Check IPv6 status
   cat /proc/net/if_inet6
   ```

2. **IPv6 Network Unreachable**
   ```bash
   # Test IPv6 connectivity
   ping6 2001:4860:4860::8888
   ```

3. **IPv6 DNS Resolution Fails**
   ```bash
   # Test IPv6 DNS
   nslookup google.com 2001:4860:4860::8888
   ```

### **IPv6 Debug Commands**

```bash
# Check IPv6 addresses
ip -6 addr show

# Test IPv6 connectivity
ping6 -c 4 2001:4860:4860::8888

# Test IPv6 DNS resolution
dig AAAA google.com @2001:4860:4860::8888

# Check IPv6 routing
ip -6 route show
```

## 📈 IPv6 Performance

### **IPv6 vs IPv4 Performance**
- **Similar Latency**: IPv6 typically has similar or better latency
- **Better Routing**: IPv6 often has more efficient routing
- **No NAT**: IPv6 eliminates NAT overhead

### **IPv6 Optimization**
- **Happy Eyeballs**: Automatic protocol selection
- **IPv6 Prefer**: Prefer IPv6 when available
- **Fallback Strategy**: Graceful fallback to IPv4

## 🎯 IPv6 Best Practices

### **Configuration Best Practices**
1. **Dual-Stack**: Always enable both IPv4 and IPv6
2. **Multiple Upstreams**: Use both IPv4 and IPv6 upstream servers
3. **Monitoring**: Monitor IPv6 performance and usage
4. **Security**: Apply security policies to both protocols

### **Deployment Best Practices**
1. **Network Planning**: Plan IPv6 address allocation
2. **Testing**: Test IPv6 functionality before production
3. **Documentation**: Document IPv6 configuration
4. **Monitoring**: Set up IPv6-specific monitoring

## 🔮 IPv6 Future

### **IPv6 Adoption Trends**
- **Growing Adoption**: Increasing IPv6 adoption worldwide
- **Mobile Networks**: Mobile networks heavily use IPv6
- **Cloud Services**: Cloud providers prioritize IPv6

### **SiNS IPv6 Roadmap**
- **Enhanced IPv6 Support**: Continued IPv6 feature development
- **IPv6-Only Mode**: Optional IPv6-only operation
- **IPv6 Analytics**: Advanced IPv6 monitoring and analytics

---

For more information about IPv6 support, see the [main documentation](../README.md) or [contact the development team](../CONTRIBUTING.md).
