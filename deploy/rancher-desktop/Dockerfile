# syntax=docker/dockerfile:1
# Local image for Rancher Desktop / k3s (build with: nerdctl -n k8s.io build -f deploy/rancher-desktop/Dockerfile -t sins-rancher:local .)

FROM node:22-alpine AS ui
WORKDIR /w
COPY sins-ui/package.json sins-ui/package-lock.json ./sins-ui/
RUN cd sins-ui && npm ci
COPY sins-ui ./sins-ui
COPY sins ./sins
RUN cd sins-ui && npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY sins/ sins/
COPY --from=ui /w/sins/wwwroot sins/wwwroot
WORKDIR /src/sins
RUN dotnet restore && dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
RUN apt-get update \
  && apt-get install -y --no-install-recommends libcap2-bin \
  && rm -rf /var/lib/apt/lists/*
COPY --from=build /out .
RUN if [ -f ./sins ]; then setcap 'cap_net_bind_service=+ep' ./sins || true; fi
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 80
EXPOSE 53/tcp
EXPOSE 53/udp
USER $APP_UID
ENTRYPOINT ["./sins"]
