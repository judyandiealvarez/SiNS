# Release Guide

This guide explains how to create releases for SiNS components, including building deb packages and publishing to GitHub Releases and APT repositories.

## Overview

SiNS has three main components that can be released:

1. **SiNS Server** - The DNS server application
2. **SiNS CLI** - Command-line interface tool
3. **Docker Image** - Pre-built container image

## Release Process

### Release

Both SiNS Server and CLI are released together in a single GitHub release with both deb packages attached.

#### Creating a Release

**Option A: Using Git Tags (Recommended)**
```bash
# Create and push a tag
git tag v1.0.0
git push origin v1.0.0
```

**Option B: Manual Workflow Dispatch**
1. Go to GitHub → Actions → "Build and Release"
2. Click "Run workflow"
3. Enter version (e.g., `1.0.0` or `v1.0.0`)
4. Click "Run workflow"

#### What Happens Automatically

1. **Build Server**: .NET server application is built and packaged (`sins_1.0.0_amd64.deb`)
2. **Build CLI**: .NET CLI application is built and packaged (`sins-cli_1.0.0_amd64.deb`)
3. **Deploy Server**: Package is uploaded to Gemfury APT repository
4. **Deploy CLI**: Package is uploaded to Gemfury APT repository
5. **GitHub Release**: Single release is created with both deb packages attached
6. **Verification**: Package availability is verified

#### Installation After Release

**SiNS Server from Gemfury:**
```bash
curl -s https://get.fury.io/judyalvarez | bash
sudo apt update
sudo apt install sins
```

**SiNS Server from GitHub Release:**
```bash
# Download from GitHub Releases page
wget https://github.com/swipentap/SiNS/releases/download/v1.0.0/sins_1.0.0_amd64.deb

# Install
sudo dpkg -i sins_1.0.0_amd64.deb
sudo apt-get install -f
```

**SiNS CLI from Gemfury:**
```bash
curl -s https://get.fury.io/judyalvarez | bash
sudo apt update
sudo apt install sins-cli
```

**SiNS CLI from GitHub Release:**
```bash
# Download from GitHub Releases page
wget https://github.com/swipentap/SiNS/releases/download/v1.0.0/sins-cli_1.0.0_amd64.deb

# Install
sudo dpkg -i sins-cli_1.0.0_amd64.deb
sudo apt-get install -f
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
docker pull swipentap/sins:1.0.0
# or
docker pull swipentap/sins:latest
```

## Version Numbering

### Semantic Versioning

Use semantic versioning: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

### Tag Formats

- **Release**: `v1.0.0` (includes both server and CLI deb packages, and Docker image)

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

### For All Releases (Gemfury)

- `GEMFURY_TOKEN`: Gemfury API token
- `GEMFURY_USER`: Gemfury username (judyalvarez)

### For Docker Releases

- `DOCKERHUB_USERNAME`: Docker Hub username
- `DOCKERHUB_TOKEN`: Docker Hub access token

## Release Artifacts

### Release Includes

- `sins_1.0.0_amd64.deb` - Server Debian package
- `sins-cli_1.0.0_amd64.deb` - CLI Debian package
- Docker image on Docker Hub
- Release notes with changelog
- Installation instructions for both packages
- Links to documentation

### Docker Release Includes

- Docker image tags (version, major.minor, latest)
- Release notes with changelog
- Docker Compose examples
- Security scan results

## Manual Release (Local Build)

If you need to build packages locally:

### Server Package

```bash
cd sins
./build-package.sh 1.0.0
# Creates: sins_1.0.0_amd64.deb
```

### CLI Package

```bash
cd sins-cli
./build-package.sh 1.0.0
# Creates: sins-cli_1.0.0_amd64.deb
```

### Upload to Gemfury (Manual)

```bash
# Upload server package
curl -F package=@sins_1.0.0_amd64.deb \
  "https://YOUR_TOKEN@push.fury.io/judyalvarez/?public=1"

# Upload CLI package
curl -F package=@sins-cli_1.0.0_amd64.deb \
  "https://YOUR_TOKEN@push.fury.io/judyalvarez/?public=1"
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

