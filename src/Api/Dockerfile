# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# Copy solution and project files for restore
COPY WcagAnalyzer.sln ./
COPY src/Api/WcagAnalyzer.Api.csproj src/Api/
COPY src/Application/WcagAnalyzer.Application.csproj src/Application/
COPY src/Domain/WcagAnalyzer.Domain.csproj src/Domain/
COPY src/Infrastructure/WcagAnalyzer.Infrastructure.csproj src/Infrastructure/
COPY src/Tests/WcagAnalyzer.Tests.csproj src/Tests/
RUN dotnet restore

# Copy all source and publish
COPY src/ src/
RUN dotnet publish src/Api/WcagAnalyzer.Api.csproj -c Release -o /app

# Install PowerShell and Playwright browsers
RUN dotnet tool install --global PowerShell
ENV PATH="$PATH:/root/.dotnet/tools"
RUN pwsh /app/playwright.ps1 install --with-deps chromium

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install Chromium dependencies needed at runtime
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 \
    libnspr4 \
    libatk1.0-0t64 \
    libatk-bridge2.0-0t64 \
    libcups2t64 \
    libdrm2 \
    libdbus-1-3 \
    libxkbcommon0 \
    libatspi2.0-0t64 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2t64 \
    libwayland-client0 \
    fonts-liberation \
    fonts-noto-color-emoji \
    && rm -rf /var/lib/apt/lists/*

# Copy Playwright browsers from build stage
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Copy published app
COPY --from=build /app .

# Create data directory for SQLite
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:5042
EXPOSE 5042

ENTRYPOINT ["dotnet", "WcagAnalyzer.Api.dll"]
