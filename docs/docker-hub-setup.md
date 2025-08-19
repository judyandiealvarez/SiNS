# Docker Hub Setup

This document explains how to set up Docker Hub integration for the SiNS project's GitHub Actions workflows.

## Overview

The SiNS project uses Docker Hub to store and distribute Docker images. The GitHub Actions workflows automatically build and push images to the `judyandiealvarez/sins` repository on Docker Hub.

## Prerequisites

1. **Docker Hub Account**: You need a Docker Hub account
2. **Repository Access**: The repository `judyandiealvarez/sins` should exist on Docker Hub
3. **GitHub Repository**: The GitHub repository should have access to set secrets

## Setup Steps

### 1. Create Docker Hub Repository

1. Log in to [Docker Hub](https://hub.docker.com/)
2. Click "Create Repository"
3. Set repository name to `sins`
4. Set visibility (public or private)
5. Click "Create"

### 2. Create Docker Hub Access Token

1. Go to your Docker Hub account settings
2. Navigate to "Security" → "Access Tokens"
3. Click "New Access Token"
4. Set token name (e.g., "GitHub Actions")
5. Set permissions:
   - **Read & Write**: For pushing images
   - **Read**: For pulling images
6. Click "Generate"
7. **Copy the token** (you won't see it again)

### 3. Add GitHub Secrets

1. Go to your GitHub repository
2. Navigate to "Settings" → "Secrets and variables" → "Actions"
3. Click "New repository secret"
4. Add the following secrets:

#### Secret: `DOCKERHUB_USERNAME`
- **Name**: `DOCKERHUB_USERNAME`
- **Value**: Your Docker Hub username (e.g., `judyandiealvarez`)

#### Secret: `DOCKERHUB_TOKEN`
- **Name**: `DOCKERHUB_TOKEN`
- **Value**: The access token you created in step 2

### 4. Verify Setup

1. Push a commit to trigger the CI workflow
2. Check the Actions tab in GitHub
3. Verify that the Docker image is built and pushed successfully
4. Check Docker Hub to see the new image

## Image Tags

The workflows create the following image tags:

- **Version tags**: `judyandiealvarez/sins:v1.0.0`
- **Branch tags**: `judyandiealvarez/sins:main`
- **PR tags**: `judyandiealvarez/sins:pr-123`
- **SHA tags**: `judyandiealvarez/sins:main-abc123`

## Usage

### Pull Images

```bash
# Pull latest release
docker pull judyandiealvarez/sins:v1.0.0

# Pull latest main branch
docker pull judyandiealvarez/sins:main
```

### Use in Docker Compose

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: dns_server
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    networks:
      dns-network:
        ipv4_address: 172.20.0.2
        
  dns-server:
    image: judyandiealvarez/sins:v1.0.0
    ports:
      - "53:53/udp"
      - "53:53/tcp"
      - "80:80"
    networks:
      dns-network:
        ipv4_address: 172.20.0.3
        
networks:
  dns-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Verify Docker Hub username and token are correct
   - Check that the token has the right permissions
   - Ensure the repository exists on Docker Hub

2. **Permission Denied**
   - Verify the repository name matches exactly
   - Check that you have write access to the repository
   - Ensure the access token has write permissions

3. **Image Not Found**
   - Check that the image was successfully pushed
   - Verify the image tag is correct
   - Check Docker Hub for the image

### Debugging

1. **Check Workflow Logs**
   - Go to Actions tab in GitHub
   - Click on the failed workflow
   - Check the "Log in to Docker Hub" step

2. **Verify Secrets**
   - Go to repository Settings → Secrets
   - Ensure both secrets are set correctly
   - Check that the secret names match exactly

3. **Test Manually**
   ```bash
   # Test Docker Hub login
   docker login -u your-username -p your-token
   
   # Test image pull
   docker pull judyandiealvarez/sins:main
   ```

## Security Considerations

1. **Access Token Security**
   - Use a dedicated access token for GitHub Actions
   - Set minimal required permissions
   - Rotate tokens regularly
   - Never commit tokens to code

2. **Repository Visibility**
   - Consider using a private repository for sensitive images
   - Public repositories are visible to everyone
   - Private repositories require authentication

3. **Image Security**
   - Regularly scan images for vulnerabilities
   - Keep base images updated
   - Use multi-stage builds to reduce image size

## Best Practices

1. **Tagging Strategy**
   - Use semantic versioning for releases
   - Keep `latest` tag updated
   - Use branch names for development builds

2. **Image Optimization**
   - Use multi-stage builds
   - Minimize layer count
   - Remove unnecessary files
   - Use appropriate base images

3. **Automation**
   - Automate image building on code changes
   - Use automated security scanning
   - Implement automated testing

## Support

If you encounter issues:

1. Check the troubleshooting section above
2. Review GitHub Actions logs
3. Verify Docker Hub repository settings
4. Check Docker Hub access token permissions
5. Contact repository maintainers if needed
