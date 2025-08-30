#!/bin/bash

# SiNS CLI Deployment Test Script
# This script tests the deployment process locally

set -e

echo "=== SiNS CLI Deployment Test ==="
echo

# Configuration
VERSION="1.0.0-test"
PACKAGE_NAME="sns"
REPO_SERVER="10.11.2.10"
REPO_USER="jaal"

echo "Testing deployment for version: $VERSION"
echo

# Step 1: Build the package
echo "1. Building Debian package..."
cd "$(dirname "$0")"
chmod +x build-package.sh
./build-package.sh "$VERSION"

if [ ! -f "sns_${VERSION}_all.deb" ]; then
    echo "❌ Package build failed!"
    exit 1
fi

echo "✅ Package built successfully: sns_${VERSION}_all.deb"
echo

# Step 2: Verify package
echo "2. Verifying package..."
echo "Package contents:"
dpkg -c "sns_${VERSION}_all.deb"

echo
echo "Package information:"
dpkg -I "sns_${VERSION}_all.deb"
echo

# Step 3: Test package installation (optional)
read -p "Do you want to test package installation locally? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "3. Testing local package installation..."
    
    # Install package locally
    sudo dpkg -i "sns_${VERSION}_all.deb"
    
    # Test the CLI
    echo "Testing CLI functionality..."
    sns --help
    
    # Uninstall package
    echo "Uninstalling test package..."
    sudo dpkg -r sns
    
    echo "✅ Local installation test completed"
    echo
fi

# Step 4: Deploy to repository (optional)
read -p "Do you want to deploy to APT repository? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "4. Deploying to APT repository..."
    
    # Check SSH connection
    echo "Testing SSH connection..."
    if ! ssh -o ConnectTimeout=5 -o BatchMode=yes "${REPO_USER}@${REPO_SERVER}" "echo 'SSH connection successful'"; then
        echo "❌ SSH connection failed! Please check your SSH configuration."
        echo "Make sure you have SSH access to ${REPO_USER}@${REPO_SERVER}"
        exit 1
    fi
    
    # Copy package to repository server
    echo "Copying package to repository server..."
    scp -o StrictHostKeyChecking=no "sns_${VERSION}_all.deb" "${REPO_USER}@${REPO_SERVER}:/tmp/"
    
    # Add package to repository
    echo "Adding package to repository..."
    ssh -o StrictHostKeyChecking=no "${REPO_USER}@${REPO_SERVER}" << EOF
        sudo /usr/local/bin/add-package.sh "/tmp/sns_${VERSION}_all.deb"
        rm "/tmp/sns_${VERSION}_all.deb"
        echo "Package added successfully!"
EOF
    
    # Verify deployment
    echo "Verifying deployment..."
    sleep 5
    
    if curl -s "http://tools.apt.home.net/dists/custom/main/binary-amd64/Packages" | grep -A 5 "Package: sns"; then
        echo "✅ Package deployed successfully!"
        echo "🌐 Repository: http://tools.apt.home.net"
        echo "📋 Install with: sudo apt update && sudo apt install sns"
    else
        echo "❌ Package not found in repository!"
        exit 1
    fi
    echo
fi

# Step 5: Cleanup
echo "5. Cleaning up..."
rm -f "sns_${VERSION}_all.deb"
rm -rf package-build/

echo "✅ Deployment test completed successfully!"
echo
echo "Next steps:"
echo "1. Review the package contents and information above"
echo "2. If satisfied, create a git tag to trigger automated deployment:"
echo "   git tag cli-v1.0.0"
echo "   git push origin cli-v1.0.0"
echo "3. Or use the manual deployment process described in DEPLOYMENT.md"
