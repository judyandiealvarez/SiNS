#!/bin/bash
set -e

# Configuration
PACKAGE_NAME="sns"
VERSION=${1:-"1.0.0"}
ARCH="all"
BUILD_DIR="package-build"
PACKAGE_DIR="${BUILD_DIR}/${PACKAGE_NAME}-${VERSION}"

echo "Building ${PACKAGE_NAME} version ${VERSION}..."

# Clean previous builds
rm -rf ${BUILD_DIR}
mkdir -p ${PACKAGE_DIR}

# Build the .NET application
echo "Building .NET application..."
dotnet publish sins-cli.csproj -c Release -o ${PACKAGE_DIR}/usr/local/bin/sns --self-contained false

# Create package structure
mkdir -p ${PACKAGE_DIR}/DEBIAN
mkdir -p ${PACKAGE_DIR}/usr/local/bin

# Copy Debian control files
cp debian/control ${PACKAGE_DIR}/DEBIAN/
cp debian/postinst ${PACKAGE_DIR}/DEBIAN/
cp debian/prerm ${PACKAGE_DIR}/DEBIAN/

# Make scripts executable
chmod +x ${PACKAGE_DIR}/DEBIAN/postinst
chmod +x ${PACKAGE_DIR}/DEBIAN/prerm

# Update version in control file
sed -i "s/Version: .*/Version: ${VERSION}/" ${PACKAGE_DIR}/DEBIAN/control

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
