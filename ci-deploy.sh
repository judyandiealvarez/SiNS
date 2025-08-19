#!/bin/bash

# SiNS CI/CD Deployment Script
# This script automatically deploys the latest SiNS version from Docker Hub

set -e

# Configuration
REPO_URL="https://github.com/judyandiealvarez/SiNS.git"
DOCKER_IMAGE="judyandiealvarez/sins:latest"
DEPLOY_DIR="/home/jaal/ci"
DOCKER_COMPOSE_FILE="$DEPLOY_DIR/docker-compose.yml"
LOG_FILE="$DEPLOY_DIR/deploy.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${BLUE}[$(date '+%Y-%m-%d %H:%M:%S')]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[$(date '+%Y-%m-%d %H:%M:%S')] SUCCESS:${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[$(date '+%Y-%m-%d %H:%M:%S')] WARNING:${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[$(date '+%Y-%m-%d %H:%M:%S')] ERROR:${NC} $1" | tee -a "$LOG_FILE"
}

# Function to check if Docker image has been updated
check_image_update() {
    log "Checking for Docker image updates..."
    
    # Get current image digest
    CURRENT_DIGEST=$(docker images --digests --format "table {{.Repository}}:{{.Tag}}\t{{.Digest}}" | grep "$DOCKER_IMAGE" | awk '{print $2}' | head -1)
    
    # Pull latest image
    log "Pulling latest image from Docker Hub..."
    docker pull "$DOCKER_IMAGE"
    
    # Get new image digest
    NEW_DIGEST=$(docker images --digests --format "table {{.Repository}}:{{.Tag}}\t{{.Digest}}" | grep "$DOCKER_IMAGE" | awk '{print $2}' | head -1)
    
    if [ "$CURRENT_DIGEST" != "$NEW_DIGEST" ]; then
        log_success "New image detected! Digest changed from $CURRENT_DIGEST to $NEW_DIGEST"
        return 0
    else
        log "No new image available. Current digest: $CURRENT_DIGEST"
        return 1
    fi
}

# Function to update docker-compose.yml
update_docker_compose() {
    log "Updating docker-compose.yml from repository..."
    
    # Download latest docker-compose.yml
    if curl -fsSL "$REPO_URL/raw/main/docker-compose.yml" -o "$DOCKER_COMPOSE_FILE.tmp"; then
        # Remove version line (obsolete warning)
        sed -i '/^version:/d' "$DOCKER_COMPOSE_FILE.tmp"
        
        # Remove volumes mount for wwwroot (we don't need it)
        sed -i '/      - .\/sins\/wwwroot:\/app\/wwwroot/d' "$DOCKER_COMPOSE_FILE.tmp"
        sed -i '/    volumes:/d' "$DOCKER_COMPOSE_FILE.tmp"
        
        # Move to final location
        mv "$DOCKER_COMPOSE_FILE.tmp" "$DOCKER_COMPOSE_FILE"
        log_success "docker-compose.yml updated successfully"
    else
        log_warning "Failed to download docker-compose.yml, using existing file"
    fi
}

# Function to deploy SiNS
deploy_sins() {
    log "Starting SiNS deployment..."
    
    # Stop existing containers
    log "Stopping existing containers..."
    cd "$DEPLOY_DIR"
    docker compose down || true
    
    # Clean up old images
    log "Cleaning up old images..."
    docker image prune -f || true
    
    # Start services
    log "Starting services with latest image..."
    docker compose up -d
    
    # Wait for services to be healthy
    log "Waiting for services to be healthy..."
    sleep 30
    
    # Check service status
    if docker compose ps | grep -q "healthy"; then
        log_success "Services are healthy!"
    else
        log_warning "Some services may not be healthy yet"
    fi
    
    # Test DNS functionality
    log "Testing DNS functionality..."
    if nslookup google.com 127.0.0.1 > /dev/null 2>&1; then
        log_success "DNS resolution test passed"
    else
        log_error "DNS resolution test failed"
        return 1
    fi
    
    # Test web interface
    log "Testing web interface..."
    if curl -fs http://127.0.0.1/ > /dev/null 2>&1; then
        log_success "Web interface test passed"
    else
        log_error "Web interface test failed"
        return 1
    fi
    
    log_success "SiNS deployment completed successfully!"
}

# Function to rollback deployment
rollback() {
    log_error "Deployment failed, attempting rollback..."
    
    cd "$DEPLOY_DIR"
    
    # Stop current containers
    docker compose down || true
    
    # Start with previous image
    log "Rolling back to previous image..."
    docker compose up -d || true
    
    log_warning "Rollback completed. Check logs for details."
}

# Main deployment function
main() {
    log "=== SiNS CI/CD Deployment Started ==="
    
    # Create deployment directory if it doesn't exist
    mkdir -p "$DEPLOY_DIR"
    cd "$DEPLOY_DIR"
    
    # Check if docker-compose.yml exists, if not download it
    if [ ! -f "$DOCKER_COMPOSE_FILE" ]; then
        log "Initial setup: downloading docker-compose.yml"
        update_docker_compose
    fi
    
    # Check for image updates
    if check_image_update; then
        log "New image detected, starting deployment..."
        
        # Update docker-compose.yml
        update_docker_compose
        
        # Deploy with error handling
        if deploy_sins; then
            log_success "=== SiNS CI/CD Deployment Completed Successfully ==="
            exit 0
        else
            log_error "Deployment failed!"
            rollback
            exit 1
        fi
    else
        log "No updates available, skipping deployment"
        exit 0
    fi
}

# Handle script arguments
case "${1:-}" in
    "force")
        log "Force deployment requested..."
        update_docker_compose
        deploy_sins
        ;;
    "status")
        log "Checking deployment status..."
        cd "$DEPLOY_DIR"
        docker compose ps
        ;;
    "logs")
        log "Showing recent deployment logs..."
        tail -50 "$LOG_FILE" 2>/dev/null || echo "No log file found"
        ;;
    "cleanup")
        log "Cleaning up old images and containers..."
        docker system prune -f
        docker image prune -f
        ;;
    *)
        main
        ;;
esac
