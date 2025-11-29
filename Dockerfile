FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Build arguments for version
ARG BUILD_NUMBER=0
ARG APP_VERSION=1.0.0

# Set environment variables
ENV BUILD_NUMBER=$BUILD_NUMBER
ENV APP_VERSION=$APP_VERSION

# Expose ports
EXPOSE 80
EXPOSE 443
EXPOSE 53

# Install prerequisites for APT repository access
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    ca-certificates \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Add Microsoft .NET repository so apt can resolve dotnet-runtime-8.0 dependency
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb

# Add Gemfury APT repository (with username in URL for public packages)
# Using [trusted=yes] because Gemfury may not provide a Release file
RUN echo "deb [trusted=yes] https://judyalvarez@apt.fury.io/judyalvarez /" | tee /etc/apt/sources.list.d/fury.list

# Update package list, install dotnet-runtime-8.0 (to satisfy sins dependency), then install sins package
# Note: Retry logic to handle Gemfury indexing delays
# The version will be passed as a build argument
RUN apt-get update && \
    apt-get install -y --no-install-recommends dotnet-runtime-8.0 && \
    (apt-get install -y --no-install-recommends sins=${APP_VERSION} || \
     (echo "Package sins=${APP_VERSION} not found, waiting for indexing..." && \
      sleep 15 && \
      apt-get update && \
      (apt-get install -y --no-install-recommends sins=${APP_VERSION} || \
       (echo "Still not found, trying latest version..." && \
        apt-get update && \
        apt-get install -y --no-install-recommends sins)))) && \
    rm -rf /var/lib/apt/lists/*

# Create necessary directories
RUN mkdir -p /etc/sins /var/log/sins /var/lib/sins

# Set capabilities for binding to port 53 (non-root)
RUN setcap 'cap_net_bind_service=+ep' /opt/sins/sins || true

# Use the installed binary
WORKDIR /opt/sins
ENTRYPOINT ["/opt/sins/sins"]
