# GitHub Actions Self-Hosted Runner Setup

This guide explains how to set up automatic deployment of SiNS to your production server using a GitHub Actions self-hosted runner.

## 🚀 Overview

The GitHub Actions self-hosted runner will automatically deploy SiNS to your server whenever changes are pushed to the `main` branch. This provides:

- **Automatic Deployment**: Deploy on every push to main
- **Manual Trigger**: Deploy manually via GitHub Actions UI
- **Health Checks**: Verify deployment success
- **Rollback Capability**: Automatic rollback on failure
- **Logging**: Comprehensive deployment logs
- **Direct Access**: Runner has direct access to server resources
- **No SSH Keys**: No need to manage SSH keys or secrets

## 🔧 Setup Instructions

### 1. Download and Extract Runner

The runner has already been downloaded to your server. If you need to reinstall:

```bash
# SSH to your server
ssh jaal@10.11.2.5

# Download runner (if needed)
cd ~/actions-runner
curl -o actions-runner-linux-x64-2.327.1.tar.gz -L https://github.com/actions/runner/releases/download/v2.327.1/actions-runner-linux-x64-2.327.1.tar.gz

# Extract runner
tar xzf ./actions-runner-linux-x64-2.327.1.tar.gz
```

### 2. Configure the Self-Hosted Runner

Run the setup script on your server:

```bash
# SSH to your server
ssh jaal@10.11.2.5

# Run the setup script
cd ~/actions-runner
sudo ./setup-github-runner.sh
```

The script will prompt you for:
- **Repository URL**: `https://github.com/swipentap/SiNS`
- **Runner Token**: Get this from GitHub repository settings

### 3. Get Runner Token from GitHub

1. Go to your GitHub repository: `https://github.com/swipentap/SiNS`
2. Navigate to: **Settings** → **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Copy the token from the configuration command

### 4. Start the Runner Service

```bash
# Start the runner service
sudo systemctl start github-runner.service

# Enable auto-start on boot
sudo systemctl enable github-runner.service

# Check status
sudo systemctl status github-runner.service
```

### 5. Test the Deployment

Once the runner is configured and running:

1. **Automatic Trigger**: Push any change to the `main` branch
2. **Manual Trigger**: Go to **Actions** → **Deploy to Production Server (Self-Hosted)** → **Run workflow**

## 📋 Workflow Details

### Trigger Conditions

- **Automatic**: Push to `main` branch
- **Manual**: Via GitHub Actions UI

### Deployment Process

1. **Checkout Code**: Get latest repository code
2. **SSH Connection**: Connect to production server
3. **Download Config**: Get latest `docker-compose.yml`
4. **Pull Image**: Download latest Docker image from Docker Hub
5. **Stop Services**: Gracefully stop existing containers
6. **Cleanup**: Remove old images
7. **Start Services**: Deploy with new image
8. **Health Checks**: Verify DNS and web interface
9. **Status Report**: Show deployment results

### Health Checks

The workflow performs these tests:

- **DNS Resolution**: `nslookup google.com 127.0.0.1`
- **Web Interface**: `curl http://127.0.0.1/`
- **Service Health**: Docker container health status

## 🔍 Monitoring and Logs

### GitHub Actions Logs

- **Real-time**: View deployment progress in GitHub Actions
- **Detailed**: Full SSH command output
- **History**: Complete deployment history

### Server Logs

Deployment logs are stored on the server:

```bash
# View deployment logs
tail -f /home/jaal/ci/deploy.log

# View recent deployments
tail -50 /home/jaal/ci/deploy.log
```

### Service Status

Check service status on the server:

```bash
# Check container status
docker compose ps

# View container logs
docker logs dns-server
docker logs dns-postgres
```

## 🛠️ Troubleshooting

### Common Issues

1. **SSH Connection Failed**
   - Verify SSH key is correct
   - Check server IP and port
   - Ensure SSH service is running

2. **Docker Image Pull Failed**
   - Check internet connectivity
   - Verify Docker Hub credentials
   - Check image name and tag

3. **Health Check Failed**
   - Check container logs
   - Verify port bindings
   - Check firewall settings

### Manual Deployment

If GitHub Actions fails, you can deploy manually:

```bash
# SSH to server
ssh jaal@10.11.2.5

# Navigate to deployment directory
cd /home/jaal/ci

# Pull latest image
docker pull swipentap/sins:latest

# Restart services
docker compose down
docker compose up -d
```

## 🔒 Security Considerations

### SSH Key Security

- **Dedicated Key**: Use separate SSH key for GitHub Actions
- **Limited Permissions**: Key should only access deployment directory
- **Regular Rotation**: Rotate SSH keys periodically

### Network Security

- **Firewall**: Restrict SSH access to GitHub Actions IPs
- **VPN**: Consider VPN for additional security
- **Monitoring**: Monitor SSH access logs

## 📈 Advanced Configuration

### Custom Deployment Directory

To change the deployment directory, modify the workflow:

```yaml
DEPLOY_DIR="/custom/path/to/deployment"
```

### Custom Health Checks

Add custom health checks to the workflow:

```bash
# Custom health check example
curl -f http://127.0.0.1/api/health
```

### Notification Integration

Add notifications for deployment status:

```yaml
- name: Notify on Success
  if: success()
  run: |
    # Add notification logic here
    echo "Deployment successful!"

- name: Notify on Failure
  if: failure()
  run: |
    # Add notification logic here
    echo "Deployment failed!"
```

## 🎯 Benefits

### Automated Workflow

- **Zero Downtime**: Graceful deployment process
- **Consistent**: Same deployment process every time
- **Reliable**: Built-in error handling and rollback

### Developer Experience

- **Simple**: Just push to main branch
- **Transparent**: Full deployment visibility
- **Fast**: Automated deployment saves time

### Production Benefits

- **Always Updated**: Latest code automatically deployed
- **Tested**: Health checks ensure functionality
- **Monitored**: Comprehensive logging and status

---

For more information, see the [main documentation](../README.md) or [contact the development team](../CONTRIBUTING.md).
