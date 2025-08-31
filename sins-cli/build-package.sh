#!/bin/bash
set -e

# Configuration
PACKAGE_NAME="sns"
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
echo "Project file exists: $(test -f sins-cli.csproj && echo 'Yes' || echo 'No')"
dotnet publish sins-cli.csproj -c Release -o ${PACKAGE_DIR}/usr/local/bin/sns --self-contained false
echo "Publish completed"

# Fix the directory structure - move files from nested directory
if [ -d "${PACKAGE_DIR}/usr/local/bin/sns/sns" ]; then
    echo "Fixing directory structure..."
    mv "${PACKAGE_DIR}/usr/local/bin/sns/sns"/* "${PACKAGE_DIR}/usr/local/bin/sns/"
    rmdir "${PACKAGE_DIR}/usr/local/bin/sns/sns"
fi

# Create package structure (DEBIAN directory)
mkdir -p ${PACKAGE_DIR}/DEBIAN

# Copy Debian control files
cp debian/control ${PACKAGE_DIR}/DEBIAN/
cp debian/postinst ${PACKAGE_DIR}/DEBIAN/
cp debian/prerm ${PACKAGE_DIR}/DEBIAN/

# Make scripts executable
chmod +x ${PACKAGE_DIR}/DEBIAN/postinst
chmod +x ${PACKAGE_DIR}/DEBIAN/prerm

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
