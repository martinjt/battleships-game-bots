FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy common project first (context is battleships-game-bots root)
COPY common/BattleshipsBot.Common/BattleshipsBot.Common.csproj common/BattleshipsBot.Common/

# Copy solution and project files
COPY bots/edgehunter/EdgeHunter.sln bots/edgehunter/
COPY bots/edgehunter/src/EdgeHunter/EdgeHunter.csproj bots/edgehunter/src/EdgeHunter/
COPY bots/edgehunter/tests/EdgeHunter.Tests/EdgeHunter.Tests.csproj bots/edgehunter/tests/EdgeHunter.Tests/

# Restore dependencies
WORKDIR /src/bots/edgehunter
RUN dotnet restore

# Copy common project source
WORKDIR /src
COPY common/BattleshipsBot.Common/ common/BattleshipsBot.Common/

# Copy bot source code
COPY bots/edgehunter/src/ bots/edgehunter/src/
COPY bots/edgehunter/tests/ bots/edgehunter/tests/

# Run tests
WORKDIR /src/bots/edgehunter
RUN dotnet test tests/EdgeHunter.Tests/EdgeHunter.Tests.csproj --configuration Release

# Build and publish
RUN dotnet publish src/EdgeHunter/EdgeHunter.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EdgeHunter.dll"]
