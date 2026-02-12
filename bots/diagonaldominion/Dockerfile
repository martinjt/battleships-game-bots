FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy common project first (context is battleships-game-bots root)
COPY common/BattleshipsBot.Common/BattleshipsBot.Common.csproj common/BattleshipsBot.Common/

# Copy solution and project files
COPY bots/diagonaldominion/Diagonaldominion.sln bots/diagonaldominion/
COPY bots/diagonaldominion/src/Diagonaldominion/Diagonaldominion.csproj bots/diagonaldominion/src/Diagonaldominion/
COPY bots/diagonaldominion/tests/Diagonaldominion.Tests/Diagonaldominion.Tests.csproj bots/diagonaldominion/tests/Diagonaldominion.Tests/

# Restore dependencies
WORKDIR /src/bots/diagonaldominion
RUN dotnet restore

# Copy common project source
WORKDIR /src
COPY common/BattleshipsBot.Common/ common/BattleshipsBot.Common/

# Copy bot source code
COPY bots/diagonaldominion/src/ bots/diagonaldominion/src/
COPY bots/diagonaldominion/tests/ bots/diagonaldominion/tests/

# Run tests
WORKDIR /src/bots/diagonaldominion
RUN dotnet test tests/Diagonaldominion.Tests/Diagonaldominion.Tests.csproj --configuration Release

# Build and publish
RUN dotnet publish src/Diagonaldominion/Diagonaldominion.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Diagonaldominion.dll"]
