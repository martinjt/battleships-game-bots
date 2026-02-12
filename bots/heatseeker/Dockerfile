FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy common project first (context is battleships-game-bots root)
COPY common/BattleshipsBot.Common/BattleshipsBot.Common.csproj common/BattleshipsBot.Common/

# Copy solution and project files
COPY bots/heatseeker/HeatSeeker.sln bots/heatseeker/
COPY bots/heatseeker/src/HeatSeeker/HeatSeeker.csproj bots/heatseeker/src/HeatSeeker/
COPY bots/heatseeker/tests/HeatSeeker.Tests/HeatSeeker.Tests.csproj bots/heatseeker/tests/HeatSeeker.Tests/

# Restore dependencies
WORKDIR /src/bots/heatseeker
RUN dotnet restore

# Copy common project source
WORKDIR /src
COPY common/BattleshipsBot.Common/ common/BattleshipsBot.Common/

# Copy bot source code
COPY bots/heatseeker/src/ bots/heatseeker/src/
COPY bots/heatseeker/tests/ bots/heatseeker/tests/

# Run tests
WORKDIR /src/bots/heatseeker
RUN dotnet test tests/HeatSeeker.Tests/HeatSeeker.Tests.csproj --configuration Release

# Build and publish
RUN dotnet publish src/HeatSeeker/HeatSeeker.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HeatSeeker.dll"]
