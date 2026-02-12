FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy common project first (context is battleships-game-bots root)
COPY common/BattleshipsBot.Common/BattleshipsBot.Common.csproj common/BattleshipsBot.Common/

# Copy solution and project files
COPY bots/csharp-shooter/SharpShooter.sln bots/csharp-shooter/
COPY bots/csharp-shooter/src/SharpShooter/SharpShooter.csproj bots/csharp-shooter/src/SharpShooter/
COPY bots/csharp-shooter/tests/SharpShooter.Tests/SharpShooter.Tests.csproj bots/csharp-shooter/tests/SharpShooter.Tests/

# Restore dependencies
WORKDIR /src/bots/csharp-shooter
RUN dotnet restore

# Copy common project source
WORKDIR /src
COPY common/BattleshipsBot.Common/ common/BattleshipsBot.Common/

# Copy bot source code
COPY bots/csharp-shooter/src/ bots/csharp-shooter/src/
COPY bots/csharp-shooter/tests/ bots/csharp-shooter/tests/

# Run tests
WORKDIR /src/bots/csharp-shooter
RUN dotnet test tests/SharpShooter.Tests/SharpShooter.Tests.csproj --configuration Release

# Build and publish
RUN dotnet publish src/SharpShooter/SharpShooter.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SharpShooter.dll"]
