# GitHub Actions Workflows

This document describes the GitHub Actions workflows used in the SiNS project for continuous integration, deployment, and security scanning.

## Overview

The SiNS project uses several GitHub Actions workflows to automate various aspects of the development and deployment process:

- **CI**: Continuous Integration for testing and building
- **Build and Release**: Automated Docker image building and release creation
- **Security Scan**: Comprehensive security scanning
- **Dependency Update**: Automated dependency updates

## Workflows

### 1. CI Workflow (`ci.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` branch

**Jobs:**
- **test**: Runs .NET tests
- **build-docker**: Builds Docker image (pushes on main branch)
- **security-scan**: Runs Trivy vulnerability scanner
- **lint**: Checks code formatting
- **dockerfile-lint**: Lints Dockerfile with Hadolint

**Features:**
- Automated testing on every PR
- Docker image building with caching
- Security scanning for container images
- Code quality checks

### 2. Build and Release Workflow (`build-and-release.yml`)

**Triggers:**
- Push of version tags (e.g., `v1.0.0`)
- Manual workflow dispatch

**Jobs:**
- **build-and-push**: Builds and pushes Docker image to GitHub Container Registry
- **create-release**: Creates GitHub release with changelog

**Features:**
- Automatic Docker image building on version tags
- Multi-platform image support
- Vulnerability scanning with Trivy
- Automated release creation with changelog
- Manual release creation capability

### 3. Security Scan Workflow (`security-scan.yml`)

**Triggers:**
- Daily at 2 AM UTC
- Manual workflow dispatch
- Push to `main` branch

**Jobs:**
- **codeql-analysis**: CodeQL security analysis for C#
- **trivy-scan**: Filesystem vulnerability scanning
- **dependency-check**: Checks for outdated and vulnerable packages
- **dockerfile-security**: Dockerfile security analysis
- **secret-scan**: Secret scanning with TruffleHog

**Features:**
- Daily security scanning
- Multiple security tools integration
- Results uploaded to GitHub Security tab

### 4. Dependency Update Workflow (`dependency-update.yml`)

**Triggers:**
- Weekly on Monday at 9 AM UTC
- Manual workflow dispatch

**Jobs:**
- **update-dependencies**: Updates .NET dependencies

**Features:**
- Automated dependency updates
- Pull request creation for updates
- Weekly scheduled updates

## Usage

### Creating a Release

1. **Tag-based Release:**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Manual Release:**
   - Go to Actions tab in GitHub
   - Select "Build and Release" workflow
   - Click "Run workflow"
   - Enter version (e.g., `v1.0.0`)
   - Click "Run workflow"

### Viewing Results

- **CI Results**: Check the Actions tab for CI workflow results
- **Security Issues**: Check the Security tab for vulnerability reports
- **Docker Images**: Available at `ghcr.io/judyandiealvarez/SiNS`
- **Releases**: Check the Releases tab for created releases

## Configuration

### Environment Variables

The workflows use the following environment variables:

- `REGISTRY`: Docker Hub (`docker.io`)
- `IMAGE_NAME`: Repository name (`judyandiealvarez/sins`)

### Secrets

The workflows use the following secrets:

- `GITHUB_TOKEN`: Automatically provided by GitHub Actions
- `DOCKERHUB_USERNAME`: Docker Hub username
- `DOCKERHUB_TOKEN`: Docker Hub access token

## Docker Images

### Image Tags

The workflows create the following image tags:

- **Version tags**: `judyandiealvarez/sins:v1.0.0`
- **Branch tags**: `judyandiealvarez/sins:main`
- **PR tags**: `judyandiealvarez/sins:pr-123`
- **SHA tags**: `judyandiealvarez/sins:main-abc123`

### Using Docker Images

```bash
# Pull latest release
docker pull judyandiealvarez/sins:v1.0.0

# Pull latest main branch
docker pull judyandiealvarez/sins:main

# Run with docker-compose
version: '3.8'
services:
  dns-server:
    image: judyandiealvarez/sins:v1.0.0
    ports:
      - "53:53/udp"
      - "53:53/tcp"
      - "80:80"
```

## Security Features

### Vulnerability Scanning

- **Trivy**: Container and filesystem vulnerability scanning
- **CodeQL**: Static code analysis for security issues
- **Hadolint**: Dockerfile security and best practices
- **TruffleHog**: Secret scanning in code

### Security Reports

All security scan results are uploaded to the GitHub Security tab, providing:

- Vulnerability details and severity
- Remediation recommendations
- Historical tracking of security issues
- Integration with GitHub's security features

## Troubleshooting

### Common Issues

1. **Build Failures:**
   - Check .NET version compatibility
   - Verify all dependencies are available
   - Check Docker build context

2. **Security Scan Failures:**
   - Review vulnerability reports
   - Update dependencies if needed
   - Address security issues in code

3. **Release Creation Failures:**
   - Verify tag format (must start with 'v')
   - Check repository permissions
   - Ensure GitHub token has required permissions

### Debugging

- Check workflow logs in the Actions tab
- Review security scan results in the Security tab
- Verify Docker image builds in the Packages tab

## Best Practices

1. **Version Management:**
   - Use semantic versioning (e.g., `v1.0.0`)
   - Tag releases consistently
   - Update changelog with each release

2. **Security:**
   - Review security scan results regularly
   - Update dependencies promptly
   - Address high-severity vulnerabilities immediately

3. **Testing:**
   - Ensure all tests pass before merging
   - Test Docker images locally before release
   - Verify functionality after dependency updates

## Integration

### GitHub Features

- **Security Tab**: Vulnerability reports and security advisories
- **Packages**: Docker image registry
- **Releases**: Automated release creation
- **Actions**: Workflow execution and monitoring

### External Tools

- **Trivy**: Vulnerability scanning
- **CodeQL**: Static analysis
- **Hadolint**: Dockerfile linting
- **TruffleHog**: Secret scanning

## Monitoring

### Workflow Status

Monitor workflow status through:

- GitHub Actions dashboard
- Repository status checks
- Branch protection rules
- Release automation

### Security Monitoring

- Daily security scans
- Vulnerability alerts
- Dependency update notifications
- Security advisory tracking
