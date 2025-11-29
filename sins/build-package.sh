#!/bin/bash
set -e

# Configuration
PACKAGE_NAME="sins"
VERSION=${1:-"1.0.0"}
ARCH="amd64"
BUILD_DIR="package-build"
PACKAGE_DIR="${BUILD_DIR}/${PACKAGE_NAME}-${VERSION}"

echo "Building ${PACKAGE_NAME} version ${VERSION}..."

# Clean previous builds
rm -rf ${BUILD_DIR}
mkdir -p ${PACKAGE_DIR}

# Build the .NET application
echo "Building .NET application..."
echo "Current directory: $(pwd)"
echo "Project file exists: $(test -f sins.csproj && echo 'Yes' || echo 'No')"
dotnet publish sins.csproj -c Release -o ${PACKAGE_DIR}/opt/sins --self-contained false
echo "Publish completed"

# Fix the directory structure - move files from nested directory if needed
if [ -d "${PACKAGE_DIR}/opt/sins/sins" ]; then
    echo "Fixing directory structure..."
    mv "${PACKAGE_DIR}/opt/sins/sins"/* "${PACKAGE_DIR}/opt/sins/"
    rmdir "${PACKAGE_DIR}/opt/sins/sins"
fi

# Create package structure
mkdir -p ${PACKAGE_DIR}/DEBIAN
mkdir -p ${PACKAGE_DIR}/etc/systemd/system
mkdir -p ${PACKAGE_DIR}/etc/sins
mkdir -p ${PACKAGE_DIR}/var/log/sins
mkdir -p ${PACKAGE_DIR}/var/lib/sins

# Copy Debian control files
cp debian/control ${PACKAGE_DIR}/DEBIAN/
cp debian/postinst ${PACKAGE_DIR}/DEBIAN/
cp debian/prerm ${PACKAGE_DIR}/DEBIAN/
cp debian/postrm ${PACKAGE_DIR}/DEBIAN/

# Copy systemd service file
cp debian/sins.service ${PACKAGE_DIR}/etc/systemd/system/sins.service

# Copy default appsettings.json to /etc/sins
if [ -f appsettings.json ]; then
    cp appsettings.json ${PACKAGE_DIR}/etc/sins/appsettings.json
else
    echo "Warning: appsettings.json not found, creating minimal config"
    cat > ${PACKAGE_DIR}/etc/sins/appsettings.json <<EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=dns_server;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "your-super-secret-jwt-key-change-this-in-production",
    "Issuer": "dns-server",
    "Audience": "dns-server-users"
  }
}
EOF
fi

# Make scripts executable
chmod +x ${PACKAGE_DIR}/DEBIAN/postinst
chmod +x ${PACKAGE_DIR}/DEBIAN/prerm
chmod +x ${PACKAGE_DIR}/DEBIAN/postrm
chmod +x ${PACKAGE_DIR}/opt/sins/sins

# Update version in control file
sed -i.bak "s/Version: .*/Version: ${VERSION}/" "${PACKAGE_DIR}/DEBIAN/control"
rm -f "${PACKAGE_DIR}/DEBIAN/control.bak"

# Create the .deb package
echo "Creating Debian package..."
dpkg-deb --build ${PACKAGE_DIR}

# Rename the package
mv ${PACKAGE_DIR}.deb ${PACKAGE_NAME}_${VERSION}_${ARCH}.deb

echo "Package created: ${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"

# Show package info
echo "Package information:"
dpkg -I ${PACKAGE_NAME}_${VERSION}_${ARCH}.deb

echo "Build completed successfully!"

