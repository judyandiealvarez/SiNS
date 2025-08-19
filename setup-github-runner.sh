#!/bin/bash

# GitHub Actions Self-Hosted Runner Setup Script
# This script sets up a self-hosted runner on the production server

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== GitHub Actions Self-Hosted Runner Setup ===${NC}"

# Configuration
RUNNER_DIR="/home/jaal/actions-runner"
SERVICE_FILE="/etc/systemd/system/github-runner.service"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}This script must be run as root (use sudo)${NC}"
    exit 1
fi

# Check if runner directory exists
if [ ! -d "$RUNNER_DIR" ]; then
    echo -e "${RED}Runner directory not found. Please download and extract the runner first.${NC}"
    exit 1
fi

# Check if runner is already configured
if [ -f "$RUNNER_DIR/.runner" ]; then
    echo -e "${YELLOW}Runner appears to be already configured.${NC}"
    read -p "Do you want to reconfigure it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${BLUE}Setup cancelled.${NC}"
        exit 0
    fi
    
    # Remove existing configuration
    echo -e "${BLUE}Removing existing runner configuration...${NC}"
    cd "$RUNNER_DIR"
    ./config.sh remove --token "$(cat .runner_token 2>/dev/null || echo '')" || true
    rm -f .runner .runner_token
fi

# Get repository URL
echo -e "${BLUE}Please provide the following information:${NC}"
read -p "GitHub Repository URL (e.g., https://github.com/judyandiealvarez/SiNS): " REPO_URL

# Extract owner and repo from URL
if [[ $REPO_URL =~ https://github\.com/([^/]+)/([^/]+) ]]; then
    OWNER="${BASH_REMATCH[1]}"
    REPO="${BASH_REMATCH[2]}"
    echo -e "${GREEN}Repository: $OWNER/$REPO${NC}"
else
    echo -e "${RED}Invalid GitHub repository URL${NC}"
    exit 1
fi

# Get runner token
echo -e "${BLUE}To get the runner token:${NC}"
echo -e "${YELLOW}1. Go to https://github.com/$OWNER/$REPO/settings/actions/runners${NC}"
echo -e "${YELLOW}2. Click 'New self-hosted runner'${NC}"
echo -e "${YELLOW}3. Copy the token from the configuration command${NC}"
echo
read -p "Enter the runner token: " RUNNER_TOKEN

# Configure the runner
echo -e "${BLUE}Configuring runner...${NC}"
cd "$RUNNER_DIR"

# Configure the runner
./config.sh \
    --url "https://github.com/$OWNER/$REPO" \
    --token "$RUNNER_TOKEN" \
    --name "sins-production-runner" \
    --labels "production,linux,x64" \
    --unattended \
    --replace

# Save token for future use
echo "$RUNNER_TOKEN" > .runner_token

# Install systemd service
echo -e "${BLUE}Installing systemd service...${NC}"
cp "$(dirname "$0")/github-runner.service" "$SERVICE_FILE"

# Reload systemd and enable service
systemctl daemon-reload
systemctl enable github-runner.service

echo -e "${GREEN}=== Setup Complete ===${NC}"
echo -e "${GREEN}Runner configured successfully!${NC}"
echo -e "${BLUE}Service: github-runner.service${NC}"
echo -e "${BLUE}Status: $(systemctl is-enabled github-runner.service)${NC}"
echo
echo -e "${YELLOW}To start the runner:${NC}"
echo -e "  sudo systemctl start github-runner.service"
echo
echo -e "${YELLOW}To check status:${NC}"
echo -e "  sudo systemctl status github-runner.service"
echo
echo -e "${YELLOW}To view logs:${NC}"
echo -e "  sudo journalctl -u github-runner.service -f"
echo
echo -e "${GREEN}The runner will now automatically start on boot and run GitHub Actions workflows!${NC}"
