# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY yt-digest-web/ .
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published app
COPY --from=build /app .

# Copy tools
COPY tools/yt-digest /usr/local/bin/yt-digest
COPY tools/yt-dlp    /usr/local/bin/yt-dlp
RUN chmod +x /usr/local/bin/yt-digest /usr/local/bin/yt-dlp

# yt-dlp needs Python and ffmpeg for some formats, plus ca-certificates for HTTPS
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    ffmpeg \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "yt-digest-web.dll"]
