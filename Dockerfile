FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 53

# Build arguments for version
ARG BUILD_NUMBER=0
ARG APP_VERSION=1.0.0

# Set environment variables
ENV BUILD_NUMBER=$BUILD_NUMBER
ENV APP_VERSION=$APP_VERSION

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["sins/sins.csproj", "sins/"]
RUN dotnet restore "sins/sins.csproj"
COPY . .
WORKDIR "/src/sins"
RUN dotnet build "sins.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sins.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sins.dll"]
