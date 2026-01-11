FROM mcr.microsoft.com/dotnet/aspnet:9.0

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
    curl \
    && rm -rf /var/lib/apt/lists/*

# Add Microsoft .NET repository (needed for apt to resolve dotnet-runtime-9.0 dependency)
# Using curl instead of wget because wget fails SSL handshake in buildkit containers
RUN curl -L https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -o packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb

# Add Gemfury APT repository (with username in URL for public packages)
# Using [trusted=yes] because Gemfury may not provide a Release file
RUN echo "deb [trusted=yes] https://judyalvarez@apt.fury.io/judyalvarez /" | tee /etc/apt/sources.list.d/fury.list

# Install sins package
# dotnet-runtime-9.0 is already in the base image, but apt needs the repo to resolve the dependency
# apt will install it as a dependency if needed, or skip if already satisfied
# Note: Retry logic to handle Gemfury indexing delays
RUN apt-get update && \
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
