# SiNS CLI Deployment Guide

This guide explains how to deploy the SiNS CLI tool to the APT repository using the provided CI/CD pipelines.

## Overview

The SiNS CLI tool is packaged as a Debian package and deployed to the custom APT repository at `http://tools.apt.home.net`. The deployment process includes:

1. Building the .NET application
2. Creating a Debian package
3. Deploying to the APT repository
4. Verifying the deployment

## Prerequisites

### Repository Access
- SSH access to the APT repository server (10.11.2.10)
- SSH key configured for automated deployments
- Access to the `add-package.sh` script on the repository server

### Build Environment
- .NET 7.0 SDK
- Debian packaging tools (`dpkg-dev`, `build-essential`, `fakeroot`)
- Git access to the repository

## CI/CD Pipeline Options

### 1. GitHub Actions (Recommended)

The GitHub Actions workflow is located at `.github/workflows/build-and-deploy-cli.yml`.

#### Triggering Deployment

**Option A: Git Tags**
```bash
# Create and push a tag to trigger deployment
git tag cli-v1.0.0
git push origin cli-v1.0.0
```

**Option B: Manual Trigger**
1. Go to GitHub repository → Actions
2. Select "Build and Deploy SiNS CLI"
3. Click "Run workflow"
4. Enter the version number (e.g., 1.0.0)
5. Click "Run workflow"

#### Required Secrets

Add these secrets to your GitHub repository:

1. **APT_REPO_SSH_KEY**: The private SSH key for accessing the repository server
   ```bash
   # Generate SSH key pair
   ssh-keygen -t ed25519 -C "ci-deployment@yourcompany.com" -f ~/.ssh/apt-repo-key
   
   # Add the private key to GitHub secrets
   cat ~/.ssh/apt-repo-key
   ```

2. **Copy the public key to the repository server**
   ```bash
   ssh-copy-id -i ~/.ssh/apt-repo-key.pub jaal@10.11.2.10
   ```

### 2. GitLab CI

The GitLab CI configuration is located at `sins-cli/.gitlab-ci.yml`.

#### Triggering Deployment

**Option A: Git Tags**
```bash
# Create and push a tag to trigger deployment
git tag cli-v1.0.0
git push origin cli-v1.0.0
```

#### Required Variables

Add these variables to your GitLab project:

1. **SSH_PRIVATE_KEY**: The private SSH key for accessing the repository server
   - Go to Settings → CI/CD → Variables
   - Add variable `SSH_PRIVATE_KEY` with the private key content
   - Mark as "Protected" and "Masked"

### 3. Jenkins Pipeline

The Jenkins pipeline is located at `sins-cli/Jenkinsfile`.

#### Setup

1. Create a new Jenkins pipeline job
2. Configure the pipeline to use the Jenkinsfile
3. Set up SSH credentials in Jenkins
4. Configure the repository URL

#### Triggering Deployment

- Manual builds: Click "Build Now"
- Automated builds: Configure webhook or polling

## Manual Deployment

If you prefer to deploy manually, follow these steps:

### 1. Build the Package

```bash
# Navigate to the CLI directory
cd sins-cli

# Make the build script executable
chmod +x build-package.sh

# Build the package (replace 1.0.0 with your version)
./build-package.sh 1.0.0
```

### 2. Deploy to Repository

```bash
# Copy package to repository server
scp sins-cli_1.0.0_all.deb jaal@10.11.2.10:/tmp/

# Add to repository
ssh jaal@10.11.2.10 << 'EOF'
  sudo /usr/local/bin/add-package.sh /tmp/sins-cli_1.0.0_all.deb
  rm /tmp/sins-cli_1.0.0_all.deb
EOF
```

### 3. Verify Deployment

```bash
# Check if package is available
curl -s http://tools.apt.home.net/dists/custom/main/binary-amd64/Packages | grep -A 5 "Package: sins-cli"

# Test installation on a client
ssh jaal@10.11.2.2 "sudo apt update && sudo apt install sins-cli"
```

## Package Structure

The Debian package includes:

```
sins-cli_1.0.0_all.deb
├── usr/local/bin/sins-cli/     # .NET application files
├── usr/local/bin/sins-cli      # Main executable
├── DEBIAN/
│   ├── control                 # Package metadata
│   ├── postinst               # Post-installation script
│   └── prerm                  # Pre-removal script
```

## Installation

After deployment, users can install the CLI tool:

```bash
# Update package list
sudo apt update

# Install the CLI tool
sudo apt install sns

# Verify installation
sns --help
```

## Version Management

### Semantic Versioning
Use semantic versioning for releases:
- `major.minor.patch` (e.g., `1.2.3`)
- Increment version for each deployment
- Use git tags to trigger deployments

### Version Extraction
The CI/CD pipelines automatically extract versions from:
- Git tags (e.g., `cli-v1.0.0` → version `1.0.0`)
- Manual input (for manual triggers)
- Build numbers (Jenkins)

## Troubleshooting

### Common Issues

1. **SSH Connection Failed**
   ```bash
   # Test SSH connection
   ssh -v jaal@10.11.2.10 "echo 'SSH working'"
   
   # Check SSH key permissions
   chmod 600 ~/.ssh/id_ed25519
   chmod 644 ~/.ssh/id_ed25519.pub
   ```

2. **Package Build Failed**
   ```bash
   # Check .NET installation
   dotnet --version
   
   # Check build tools
   dpkg -l | grep dpkg-dev
   ```

3. **Package Not Found After Deployment**
   ```bash
   # Check repository status
   curl -s http://tools.apt.home.net/dists/custom/Release
   
   # Check package availability
   curl -s http://tools.apt.home.net/dists/custom/main/binary-amd64/Packages | grep sins-cli
   ```

4. **Installation Fails**
   ```bash
   # Check package dependencies
   dpkg -I sins-cli_1.0.0_all.deb | grep Depends
   
   # Check package structure
   dpkg -c sins-cli_1.0.0_all.deb
   ```

### Repository Health Check

```bash
# Check repository status
curl -s http://tools.apt.home.net/dists/custom/Release

# List all packages
curl -s http://tools.apt.home.net/dists/custom/main/binary-amd64/Packages | grep Package:

# Test client access
ssh jaal@10.11.2.2 "sudo apt update && apt list --upgradable | grep custom"
```

## Security Considerations

### SSH Key Management
- Use dedicated SSH keys for CI/CD deployments
- Rotate keys regularly
- Limit key permissions on the repository server
- Monitor repository access logs

### Package Security
- Verify package contents before deployment
- Test packages in a staging environment
- Monitor for security vulnerabilities in dependencies

## Best Practices

1. **Automated Testing**: Always test the CLI tool before deployment
2. **Version Control**: Use git tags for version management
3. **Rollback Strategy**: Keep previous package versions for rollback
4. **Monitoring**: Monitor repository health and client installations
5. **Documentation**: Update documentation with each release

## Integration with Main Project

The CLI deployment is integrated with the main SiNS project:

1. **Shared Repository**: Uses the same git repository as the main project
2. **Version Coordination**: CLI versions can be coordinated with main project releases
3. **Documentation**: Deployment documentation is included in the project
4. **CI/CD Integration**: Can be triggered alongside main project deployments

## Support

For deployment issues:

1. Check the troubleshooting section
2. Review CI/CD pipeline logs
3. Verify repository server status
4. Test manual deployment process
5. Contact the infrastructure team

## Future Enhancements

Planned improvements:

1. **Multi-architecture Support**: Build packages for different architectures
2. **Automated Testing**: Add comprehensive test suites
3. **Rollback Automation**: Automated rollback capabilities
4. **Monitoring Integration**: Integration with monitoring systems
5. **Security Scanning**: Automated security vulnerability scanning
