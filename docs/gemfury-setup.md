# Gemfury APT Repository Setup

This guide explains how to set up and use Gemfury for hosting SiNS server and CLI deb packages.

## Repository Information

- **Service**: Gemfury (https://gemfury.com)
- **Username**: judyalvarez
- **Repository URL**: `https://apt.fury.io/judyalvarez/`

## Setting Up GitHub Secrets

To enable automated deployment via GitHub Actions, you need to add the following secrets to your GitHub repository:

### Steps to Add Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add the following secrets:

#### Required Secrets

1. **GEMFURY_TOKEN**
   - Value: `28tdVK-e22ubAHyDPttWkw9Hw4GcZaGzQ`
   - Description: Gemfury API token for uploading packages

2. **GEMFURY_USER**
   - Value: `judyalvarez`
   - Description: Gemfury username/account name

### Security Notes

- ✅ Secrets are encrypted and never exposed in logs
- ✅ Only accessible to GitHub Actions workflows
- ✅ Can be restricted to specific environments
- ❌ Never commit tokens to the repository

## Manual Package Upload

If you want to upload packages manually:

```bash
# Upload a package
curl -F package=@sins-server_1.0.0_amd64.deb \
  https://28tdVK-e22ubAHyDPttWkw9Hw4GcZaGzQ@push.fury.io/judyalvarez/

# Upload CLI package
curl -F package=@sns_1.0.0_amd64.deb \
  https://28tdVK-e22ubAHyDPttWkw9Hw4GcZaGzQ@push.fury.io/judyalvarez/
```

## Installing Packages from Gemfury

### Option 1: Using Gemfury Install Script

```bash
# Add repository and install
curl -s https://get.fury.io/judyalvarez | bash
sudo apt update
sudo apt install sins-server sns
```

### Option 2: Manual Repository Setup

```bash
# Add repository manually
echo "deb https://judyalvarez@apt.fury.io/judyalvarez /" | sudo tee /etc/apt/sources.list.d/fury.list

# Update package list
sudo apt update

# Install packages
sudo apt install sins-server
sudo apt install sns
```

### Option 3: Using Token in Repository URL

If you need to use the token for authentication:

```bash
# Add repository with token
echo "deb https://28tdVK-e22ubAHyDPttWkw9Hw4GcZaGzQ@apt.fury.io/judyalvarez /" | sudo tee /etc/apt/sources.list.d/fury.list

sudo apt update
sudo apt install sins-server sns
```

## Verifying Package Availability

```bash
# Check if packages are available
curl -s https://apt.fury.io/judyalvarez/Packages | grep -E "Package: (sins-server|sns)"

# List all packages in repository
curl -s https://apt.fury.io/judyalvarez/Packages | grep "^Package:"
```

## CI/CD Integration

The GitHub Actions workflows automatically:
1. Build the deb packages
2. Upload to Gemfury using secrets
3. Verify deployment

### Triggering Deployment

**For Server:**
```bash
git tag server-v1.0.0
git push origin server-v1.0.0
```

**For CLI:**
```bash
git tag cli-v1.0.0
git push origin cli-v1.0.0
```

## Troubleshooting

### Package Not Found After Upload

- Wait a few minutes for Gemfury to index the package
- Verify the package was uploaded: Check Gemfury dashboard
- Check package name matches in repository

### Authentication Issues

- Verify GEMFURY_TOKEN secret is set correctly
- Check token hasn't expired (regenerate if needed)
- Ensure GEMFURY_USER matches your Gemfury account

### Installation Fails

```bash
# Update package list
sudo apt update

# Check repository is accessible
curl -s https://apt.fury.io/judyalvarez/Packages | head -20

# Verify package exists
apt-cache search sins-server
```

## Repository Management

### View Packages in Gemfury Dashboard

1. Go to https://gemfury.com
2. Login with your account
3. Navigate to your repository
4. View uploaded packages

### Remove Package

Use Gemfury web interface to remove packages if needed.

## Best Practices

1. **Version Management**: Use semantic versioning (e.g., 1.0.0)
2. **Testing**: Test packages locally before uploading
3. **Documentation**: Keep this guide updated with any changes
4. **Security**: Never commit tokens to the repository
5. **Monitoring**: Check Gemfury dashboard regularly for upload status

