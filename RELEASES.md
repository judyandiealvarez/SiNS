# Release Guide

This guide explains how to create releases for SiNS components, including building deb packages and publishing to GitHub Releases and APT repositories.

## Overview

SiNS has three main components that can be released:

1. **SiNS Server** - The DNS server application
2. **SiNS CLI (sns)** - Command-line interface tool
3. **Docker Image** - Pre-built container image

## Release Process

### 1. SiNS Server Release

The server is packaged as a Debian package and published to:
- **Gemfury APT Repository**: `https://apt.fury.io/judyalvarez/`
- **GitHub Releases**: With deb package attached

#### Creating a Server Release

**Option A: Using Git Tags (Recommended)**
```bash
# Create and push a tag
git tag server-v1.0.0
git push origin server-v1.0.0
```

**Option B: Manual Workflow Dispatch**
1. Go to GitHub → Actions → "Build and Deploy SiNS Server"
2. Click "Run workflow"
3. Enter version (e.g., `1.0.0`)
4. Click "Run workflow"

#### What Happens Automatically

1. **Build**: .NET application is built in Release mode
2. **Package**: Debian package is created (`sins-server_1.0.0_amd64.deb`)
3. **Deploy to Gemfury**: Package is uploaded to Gemfury APT repository
4. **GitHub Release**: Release is created with deb package attached
5. **Verification**: Package availability is verified

#### Installation After Release

**From Gemfury:**
```bash
curl -s https://get.fury.io/judyalvarez | bash
sudo apt update
sudo apt install sins-server
```

**From GitHub Release:**
```bash
# Download deb package from GitHub Releases page
wget https://github.com/judyandiealvarez/SiNS/releases/download/server-v1.0.0/sins-server_1.0.0_amd64.deb

# Install
sudo dpkg -i sins-server_1.0.0_amd64.deb
sudo apt-get install -f  # Install dependencies if needed
```

### 2. SiNS CLI Release

The CLI tool is packaged as a Debian package and published to:
- **APT Repository**: `http://tools.apt.home.net`
- **GitHub Releases**: With deb package attached

#### Creating a CLI Release

**Option A: Using Git Tags (Recommended)**
```bash
# Create and push a tag
git tag cli-v1.0.0
git push origin cli-v1.0.0
```

**Option B: Manual Workflow Dispatch**
1. Go to GitHub → Actions → "Build and Deploy SiNS CLI (sns)"
2. Click "Run workflow"
3. Enter version (e.g., `1.0.0`)
4. Click "Run workflow"

#### What Happens Automatically

1. **Build**: .NET application is built in Release mode
2. **Test**: CLI help command is tested
3. **Package**: Debian package is created (`sns_1.0.0_amd64.deb`)
4. **Deploy to APT**: Package is uploaded to APT repository
5. **GitHub Release**: Release is created with deb package attached
6. **Verification**: Package availability is verified

#### Installation After Release

**From APT Repository:**
```bash
echo "deb http://tools.apt.home.net /" | sudo tee /etc/apt/sources.list.d/custom.list
sudo apt update
sudo apt install sns
```

**From GitHub Release:**
```bash
# Download deb package from GitHub Releases page
wget https://github.com/judyandiealvarez/SiNS/releases/download/cli-v1.0.0/sns_1.0.0_amd64.deb

# Install
sudo dpkg -i sns_1.0.0_amd64.deb
sudo apt-get install -f  # Install dependencies if needed
```

### 3. Docker Image Release

The Docker image is published to Docker Hub.

#### Creating a Docker Release

```bash
# Create and push a tag
git tag v1.0.0
git push origin v1.0.0
```

#### What Happens Automatically

1. **Build**: Docker image is built
2. **Push to Docker Hub**: Image is pushed with multiple tags
3. **Security Scan**: Trivy vulnerability scan
4. **GitHub Release**: Release is created with Docker image information

#### Installation After Release

```bash
docker pull judyandiealvarez/sins:1.0.0
# or
docker pull judyandiealvarez/sins:latest
```

## Version Numbering

### Semantic Versioning

Use semantic versioning: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

### Tag Formats

- **Server**: `server-v1.0.0`
- **CLI**: `cli-v1.0.0`
- **Docker**: `v1.0.0`

## Release Checklist

Before creating a release:

- [ ] All tests pass
- [ ] Version numbers updated in code
- [ ] Changelog updated
- [ ] Documentation updated
- [ ] Dependencies checked
- [ ] Security vulnerabilities scanned

## GitHub Secrets Required

### For Server Releases (Gemfury)

- `GEMFURY_TOKEN`: Gemfury API token
- `GEMFURY_USER`: Gemfury username (judyalvarez)

### For CLI Releases (APT Repository)

- `APT_REPO_SSH_KEY`: SSH private key for APT repository server

### For Docker Releases

- `DOCKERHUB_USERNAME`: Docker Hub username
- `DOCKERHUB_TOKEN`: Docker Hub access token

## Release Artifacts

### Server Release Includes

- `sins-server_1.0.0_amd64.deb` - Debian package
- Release notes with changelog
- Installation instructions
- Links to documentation

### CLI Release Includes

- `sns_1.0.0_amd64.deb` - Debian package
- Release notes with changelog
- Installation instructions
- Usage examples

### Docker Release Includes

- Docker image tags (version, major.minor, latest)
- Release notes with changelog
- Docker Compose examples
- Security scan results

## Troubleshooting

### Release Failed

1. Check GitHub Actions logs
2. Verify secrets are set correctly
3. Check tag format matches workflow triggers
4. Ensure build environment has required tools

### Package Not Available

- **Gemfury**: Wait a few minutes for indexing
- **APT Repository**: Check SSH connection and permissions
- **GitHub Release**: Check workflow completed successfully

### Installation Issues

```bash
# Check package dependencies
dpkg -I package.deb | grep Depends

# Install missing dependencies
sudo apt-get install -f

# Verify package integrity
dpkg -c package.deb
```

## Manual Release (Local Build)

If you need to build packages locally:

### Server Package

```bash
cd sins
./build-package.sh 1.0.0
# Creates: sins-server_1.0.0_amd64.deb
```

### CLI Package

```bash
cd sins-cli
./build-package.sh 1.0.0
# Creates: sns_1.0.0_amd64.deb
```

### Upload to Gemfury (Manual)

```bash
curl -F package=@sins-server_1.0.0_amd64.deb \
  https://YOUR_TOKEN@push.fury.io/judyalvarez/
```

## Best Practices

1. **Test Before Release**: Always test packages locally
2. **Version Consistency**: Keep versions consistent across components
3. **Release Notes**: Include meaningful changelog
4. **Documentation**: Update docs with new features
5. **Security**: Scan for vulnerabilities before release
6. **Rollback Plan**: Keep previous versions available

## Related Documentation

- [Gemfury Setup](docs/gemfury-setup.md)
- [CLI Deployment](sins-cli/DEPLOYMENT.md)
- [Docker Hub Overview](docs/dockerhub-overview.md)
- [Installation Guide](docs/installation.md)

