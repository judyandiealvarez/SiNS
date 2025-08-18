#!/bin/bash

# DNS Server Startup Script
# This script helps you start, stop, and manage the DNS server

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
}

# Function to check if Docker Compose is available
check_docker_compose() {
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed. Please install Docker Compose and try again."
        exit 1
    fi
}

# Function to start the DNS server
start() {
    print_status "Starting DNS Server..."
    check_docker
    check_docker_compose
    
    # Build and start services
    docker-compose up -d --build
    
    print_status "DNS Server is starting up..."
    print_status "Web interface will be available at: http://localhost"
    print_status "Default login credentials: admin / admin123"
    print_status "DNS server will be available on port 53 (UDP/TCP)"
    
    # Wait a moment for services to start
    sleep 5
    
    # Check if services are running
    if docker-compose ps | grep -q "Up"; then
        print_status "DNS Server is running successfully!"
    else
        print_error "Failed to start DNS Server. Check logs with: docker-compose logs"
        exit 1
    fi
}

# Function to stop the DNS server
stop() {
    print_status "Stopping DNS Server..."
    docker-compose down
    print_status "DNS Server stopped."
}

# Function to restart the DNS server
restart() {
    print_status "Restarting DNS Server..."
    stop
    start
}

# Function to show status
status() {
    print_status "DNS Server Status:"
    docker-compose ps
}

# Function to show logs
logs() {
    print_status "Showing DNS Server logs..."
    docker-compose logs -f
}

# Function to show help
show_help() {
    echo "DNS Server Management Script"
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  start     Start the DNS server"
    echo "  stop      Stop the DNS server"
    echo "  restart   Restart the DNS server"
    echo "  status    Show the status of services"
    echo "  logs      Show logs from all services"
    echo "  help      Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 start    # Start the DNS server"
    echo "  $0 logs     # View logs"
    echo "  $0 stop     # Stop the DNS server"
}

# Main script logic
case "${1:-help}" in
    start)
        start
        ;;
    stop)
        stop
        ;;
    restart)
        restart
        ;;
    status)
        status
        ;;
    logs)
        logs
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        print_error "Unknown command: $1"
        echo ""
        show_help
        exit 1
        ;;
esac
