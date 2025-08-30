# CI/CD APT Repository Deployment Guide

This guide explains how to deploy packages to the custom APT repository (`tools.apt.home.net`) in CI/CD pipelines.

## Repository Information

- **Repository URL**: `http://tools.apt.home.net`
- **Repository Server**: VM 2010 (10.11.2.10)
- **Repository Path**: `/var/www/repos/custom-simple`
- **Web Server**: Apache2 on port 8080
- **HAProxy**: Routes `tools.apt.home.net` → `10.11.2.10:8080`

## Prerequisites

### 1. Package Requirements
- Package must be a valid `.deb` file
- Package should follow Debian packaging standards
- Package should have proper dependencies defined

### 2. Build Environment
- Ubuntu/Debian-based build environment
- `dpkg-dev` package installed
- `reprepro` or `dpkg-scanpackages` available

## CI/CD Pipeline Integration

### GitHub Actions Example

```yaml
name: Build and Deploy Package

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup build environment
      run: |
        sudo apt update
        sudo apt install -y dpkg-dev build-essential fakeroot
        
    - name: Build package
      run: |
        # Your package building commands here
        # Example for a simple package:
        mkdir -p mytool-1.0.0/usr/local/bin
        echo '#!/bin/bash' > mytool-1.0.0/usr/local/bin/mytool
        echo 'echo "Hello from mytool!"' >> mytool-1.0.0/usr/local/bin/mytool
        chmod +x mytool-1.0.0/usr/local/bin/mytool
        
        # Create control file
        mkdir -p mytool-1.0.0/DEBIAN
        cat > mytool-1.0.0/DEBIAN/control << EOF
        Package: mytool
        Version: 1.0.0
        Architecture: all
        Maintainer: Your Name <your.email@example.com>
        Description: A custom tool
         This is a custom tool built for our infrastructure.
        EOF
        
        # Build .deb package
        dpkg-deb --build mytool-1.0.0
        
    - name: Deploy to APT Repository
      run: |
        # Copy package to repository server
        scp -o StrictHostKeyChecking=no mytool-1.0.0.deb jaal@10.11.2.10:/tmp/
        
        # Add package to repository
        ssh -o StrictHostKeyChecking=no jaal@10.11.2.10 << 'EOF'
          sudo /usr/local/bin/add-package.sh /tmp/mytool-1.0.0.deb
          rm /tmp/mytool-1.0.0.deb
        EOF
        
    - name: Notify deployment
      run: |
        echo "Package deployed to http://tools.apt.home.net"
        echo "Install with: sudo apt update && sudo apt install mytool"
```

### GitLab CI Example

```yaml
stages:
  - build
  - deploy

build_package:
  stage: build
  image: ubuntu:24.04
  before_script:
    - apt update && apt install -y dpkg-dev build-essential fakeroot
  script:
    - # Your package building commands here
    - echo "Building package..."
    - dpkg-deb --build mytool-1.0.0
  artifacts:
    paths:
      - "*.deb"
    expire_in: 1 hour

deploy_to_apt:
  stage: deploy
  image: alpine:latest
  before_script:
    - apk add --no-cache openssh-client
    - eval $(ssh-agent -s)
    - echo "$SSH_PRIVATE_KEY" | tr -d '\r' | ssh-add -
    - mkdir -p ~/.ssh
    - chmod 700 ~/.ssh
  script:
    - scp -o StrictHostKeyChecking=no *.deb jaal@10.11.2.10:/tmp/
    - |
      ssh -o StrictHostKeyChecking=no jaal@10.11.2.10 << 'EOF'
        for deb in /tmp/*.deb; do
          sudo /usr/local/bin/add-package.sh "$deb"
        done
        rm /tmp/*.deb
      EOF
  only:
    - tags
```

### Jenkins Pipeline Example

```groovy
pipeline {
    agent any
    
    stages {
        stage('Build Package') {
            steps {
                sh '''
                    sudo apt update
                    sudo apt install -y dpkg-dev build-essential fakeroot
                    
                    # Your package building commands here
                    echo "Building package..."
                    dpkg-deb --build mytool-1.0.0
                '''
            }
        }
        
        stage('Deploy to APT Repository') {
            steps {
                script {
                    // Copy package to repository server
                    sh 'scp -o StrictHostKeyChecking=no *.deb jaal@10.11.2.10:/tmp/'
                    
                    // Add package to repository
                    sh '''
                        ssh -o StrictHostKeyChecking=no jaal@10.11.2.10 << 'EOF'
                            for deb in /tmp/*.deb; do
                                sudo /usr/local/bin/add-package.sh "$deb"
                            done
                            rm /tmp/*.deb
                        EOF
                    '''
                }
            }
        }
    }
    
    post {
        success {
            echo "Package successfully deployed to http://tools.apt.home.net"
        }
    }
}
```

## Manual Deployment

### 1. Build Package Locally

```bash
# Install build tools
sudo apt update
sudo apt install -y dpkg-dev build-essential fakeroot

# Build your package
dpkg-deb --build your-package-1.0.0
```

### 2. Deploy to Repository

```bash
# Copy package to repository server
scp your-package-1.0.0.deb jaal@10.11.2.10:/tmp/

# Add to repository
ssh jaal@10.11.2.10 << 'EOF'
  sudo /usr/local/bin/add-package.sh /tmp/your-package-1.0.0.deb
  rm /tmp/your-package-1.0.0.deb
EOF
```

### 3. Verify Deployment

```bash
# Check if package is available
curl -s http://tools.apt.home.net/dists/custom/main/binary-amd64/Packages | grep your-package

# Test installation on a client VM
ssh jaal@10.11.2.2 "sudo apt update && sudo apt install your-package"
```

## Security Considerations

### SSH Key Authentication
For automated deployments, use SSH key authentication:

```bash
# Generate SSH key pair
ssh-keygen -t ed25519 -C "ci-deployment@yourcompany.com"

# Copy public key to repository server
ssh-copy-id -i ~/.ssh/id_ed25519.pub jaal@10.11.2.10

# Add private key to CI/CD secrets
# GitHub: Settings → Secrets and variables → Actions → New repository secret
# GitLab: Settings → CI/CD → Variables
# Jenkins: Credentials → Add credentials
```

### Repository Access Control
- Repository server (VM 2010) should only accept connections from authorized CI/CD systems
- Consider using a dedicated deployment user with limited permissions
- Monitor repository access logs

## Package Versioning

### Semantic Versioning
Use semantic versioning for your packages:
- `major.minor.patch` (e.g., `1.2.3`)
- Increment version for each deployment
- Use git tags to trigger deployments

### Version Management
```bash
# Extract version from git tag
VERSION=$(git describe --tags --abbrev=0 | sed 's/v//')

# Update package version
sed -i "s/Version: .*/Version: $VERSION/" package/DEBIAN/control
```

## Troubleshooting

### Common Issues

1. **Package not found after deployment**
   ```bash
   # Check if package was added correctly
   ssh jaal@10.11.2.10 "ls -la /var/www/repos/custom-simple/pool/main/c/"
   
   # Regenerate package index
   ssh jaal@10.11.2.10 "sudo /usr/local/bin/add-package.sh /tmp/test.deb"
   ```

2. **SSH connection issues**
   ```bash
   # Test SSH connection
   ssh -v jaal@10.11.2.10 "echo 'SSH working'"
   
   # Check SSH key permissions
   chmod 600 ~/.ssh/id_ed25519
   chmod 644 ~/.ssh/id_ed25519.pub
   ```

3. **Package installation fails**
   ```bash
   # Check package dependencies
   dpkg -I your-package.deb | grep Depends
   
   # Check package structure
   dpkg -c your-package.deb
   ```

### Repository Health Check

```bash
# Check repository status
curl -s http://tools.apt.home.net/dists/custom/Release

# Check available packages
curl -s http://tools.apt.home.net/dists/custom/main/binary-amd64/Packages | grep Package:

# Test client access
ssh jaal@10.11.2.2 "sudo apt update && apt list --upgradable | grep custom"
```

## Best Practices

1. **Automated Testing**: Test packages before deployment
2. **Rollback Strategy**: Keep previous package versions
3. **Monitoring**: Monitor repository health and client installations
4. **Documentation**: Document package dependencies and installation requirements
5. **Backup**: Regularly backup the repository contents

## Repository Management

### List All Packages
```bash
ssh jaal@10.11.2.10 "ls -la /var/www/repos/custom-simple/pool/main/c/"
```

### Remove Package
```bash
# Remove package file
ssh jaal@10.11.2.10 "sudo rm /var/www/repos/custom-simple/pool/main/c/your-package_1.0.0_amd64.deb"

# Regenerate index
ssh jaal@10.11.2.10 "cd /var/www/repos/custom-simple && sudo dpkg-scanpackages pool/ > dists/custom/main/binary-amd64/Packages"
```

### Repository Backup
```bash
# Backup repository
ssh jaal@10.11.2.10 "sudo tar -czf /tmp/apt-repo-backup-$(date +%Y%m%d).tar.gz /var/www/repos/custom-simple"

# Download backup
scp jaal@10.11.2.10:/tmp/apt-repo-backup-*.tar.gz ./
```

## Integration with Existing Workflows

### Docker Image Deployment
```yaml
# Deploy both Docker image and APT package
- name: Build and push Docker image
  run: |
    docker build -t your-registry/your-app:${{ github.sha }} .
    docker push your-registry/your-app:${{ github.sha }}
    
- name: Build and deploy APT package
  run: |
    # Build .deb package
    dpkg-deb --build your-app-1.0.0
    
    # Deploy to APT repository
    scp your-app-1.0.0.deb jaal@10.11.2.10:/tmp/
    ssh jaal@10.11.2.10 "sudo /usr/local/bin/add-package.sh /tmp/your-app-1.0.0.deb"
```

### Multi-Architecture Support
```bash
# Build for multiple architectures
for arch in amd64 arm64; do
    dpkg-deb --build your-package-1.0.0-$arch
    scp your-package-1.0.0-$arch.deb jaal@10.11.2.10:/tmp/
    ssh jaal@10.11.2.10 "sudo /usr/local/bin/add-package.sh /tmp/your-package-1.0.0-$arch.deb"
done
```

This guide provides comprehensive instructions for integrating APT repository deployment into your CI/CD pipelines, ensuring reliable and automated package distribution across your infrastructure.
