#!/bin/bash

# DNS Server Deployment Script
# This script stops the system DNS resolver and deploys our custom DNS server

set -e

echo "=== SiNS DNS Server Deployment Script ==="
echo "This script will:"
echo "1. Stop system DNS resolver services"
echo "2. Deploy SiNS DNS server"
echo "3. Configure network settings"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root (use sudo)"
   exit 1
fi

# Function to stop system DNS services
stop_system_dns() {
    echo "Stopping system DNS resolver services..."
    
    # Stop systemd-resolved (Ubuntu/Debian)
    if systemctl is-active --quiet systemd-resolved; then
        echo "Stopping systemd-resolved..."
        systemctl stop systemd-resolved
        systemctl disable systemd-resolved
    fi
    
    # Stop NetworkManager DNS (if using NetworkManager)
    if systemctl is-active --quiet NetworkManager; then
        echo "Configuring NetworkManager to not manage DNS..."
        # This prevents NetworkManager from overriding our DNS settings
    fi
    
    # Stop other common DNS services
    for service in named bind9 dnsmasq; do
        if systemctl is-active --quiet $service; then
            echo "Stopping $service..."
            systemctl stop $service
            systemctl disable $service
        fi
    done
    
    echo "System DNS services stopped."
}

# Function to configure network DNS
configure_network_dns() {
    echo "Configuring network DNS settings..."
    
    # Set localhost as primary DNS
    echo "Setting localhost (127.0.0.1) as primary DNS..."
    
    # For Ubuntu/Debian with systemd-resolved disabled
    if [ -f /etc/resolv.conf ]; then
        cp /etc/resolv.conf /etc/resolv.conf.backup
        echo "nameserver 127.0.0.1" > /etc/resolv.conf
        echo "nameserver 8.8.8.8" >> /etc/resolv.conf
        echo "nameserver 1.1.1.1" >> /etc/resolv.conf
        chattr +i /etc/resolv.conf 2>/dev/null || true
    fi
    
    echo "Network DNS configured."
}

# Function to deploy DNS server
deploy_dns_server() {
    echo "Deploying SiNS DNS server..."
    
    # Stop existing containers
    echo "Stopping existing containers..."
    docker-compose down 2>/dev/null || true
    
    # Build and start services
    echo "Building and starting SiNS DNS server..."
    docker-compose up -d --build
    
    # Wait for services to be healthy
    echo "Waiting for services to be healthy..."
    timeout=120
    counter=0
    
    while [ $counter -lt $timeout ]; do
        if docker-compose ps | grep -q "healthy"; then
            echo "All services are healthy!"
            break
        fi
        echo "Waiting for services to be healthy... ($counter/$timeout)"
        sleep 5
        counter=$((counter + 5))
    done
    
    if [ $counter -ge $timeout ]; then
        echo "Warning: Services may not be fully healthy yet"
    fi
}

# Function to test DNS server
test_dns_server() {
    echo "Testing SiNS DNS server..."
    
    # Wait a moment for DNS server to be ready
    sleep 5
    
    # Test DNS resolution
    echo "Testing DNS resolution..."
    if nslookup google.com 127.0.0.1 >/dev/null 2>&1; then
        echo "✅ DNS resolution test passed"
    else
        echo "❌ DNS resolution test failed"
        echo "You may need to wait a moment for the DNS server to fully start"
    fi
    
    # Test web interface
    echo "Testing web interface..."
    if curl -s http://localhost >/dev/null; then
        echo "✅ Web interface is accessible at http://localhost"
        echo "Default login: admin / admin123"
    else
        echo "❌ Web interface test failed"
    fi
}

# Function to show status
show_status() {
    echo ""
    echo "=== Deployment Status ==="
    echo "Container status:"
    docker-compose ps
    
    echo ""
    echo "Network configuration:"
    echo "SiNS DNS Server IP: 172.20.0.3"
    echo "PostgreSQL IP: 172.20.0.2"
    echo "Network: 172.20.0.0/16"
    
    echo ""
    echo "Port mappings:"
    echo "Web Interface: http://localhost"
    echo "DNS (UDP/TCP): localhost:53"
    
    echo ""
    echo "To check DNS resolution:"
    echo "nslookup google.com 127.0.0.1"
    echo "dig @127.0.0.1 google.com"
}

# Main deployment process
main() {
    echo "Starting DNS server deployment..."
    
    # Stop system DNS services
    stop_system_dns
    
    # Configure network DNS
    configure_network_dns
    
    # Deploy DNS server
    deploy_dns_server
    
    # Test deployment
    test_dns_server
    
    # Show status
    show_status
    
    echo ""
    echo "=== Deployment Complete ==="
    echo "Your SiNS DNS server is now running!"
    echo "Web interface: http://localhost"
    echo "DNS server: 127.0.0.1:53"
}

# Run main function
main "$@"
