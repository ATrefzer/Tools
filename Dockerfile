# Build stage for yt-digest (the CLI summarization tool)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-yt-digest
WORKDIR /src
COPY yt-digest/ .
RUN dotnet publish -c Release -o /yt-digest-out

# Build stage for yt-digest-web (the web frontend)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-web
WORKDIR /src
COPY yt-digest-web/ .
RUN dotnet publish -c Release -o /web-out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published web app
COPY --from=build-web /web-out .

# Copy yt-digest binary
COPY --from=build-yt-digest /yt-digest-out/yt-digest /usr/local/bin/yt-digest
RUN chmod +x /usr/local/bin/yt-digest

# Download yt-dlp for Linux x86_64.
# For other platforms use the appropriate binary from:
# https://github.com/yt-dlp/yt-dlp/releases
# Linux x86_64 : yt-dlp
# Linux aarch64: yt-dlp_linux_aarch64
# Windows      : yt-dlp.exe
# macOS        : yt-dlp_macos
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
    -o /usr/local/bin/yt-dlp \
    && chmod +x /usr/local/bin/yt-dlp

# yt-dlp dependencies: python3 and ffmpeg for media processing, ca-certificates for HTTPS
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    ffmpeg \
    ca-certificates \
    curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "yt-digest-web.dll"]
