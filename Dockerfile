# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Copy all projects and code
COPY . .

# Build Daemon
WORKDIR "/src/TelegramProxy.Daemon"
RUN dotnet publish "TelegramProxy.Daemon.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS runtime
WORKDIR /app

# Install cloudflared natively for Linux
RUN apt-get update && apt-get install -y wget \
    && wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64 \
    && chmod +x cloudflared-linux-amd64 \
    && mv cloudflared-linux-amd64 /usr/local/bin/cloudflared \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The exposed LocalPort in config.json is 8080 by default
EXPOSE 8080

ENTRYPOINT ["dotnet", "TelegramProxy.Daemon.dll"]
