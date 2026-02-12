FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy common project first (context is battleships-game-bots root)
COPY common/BattleshipsBot.Common/BattleshipsBot.Common.csproj common/BattleshipsBot.Common/

# Copy solution and project files
COPY bots/stackoverflowattack/StackOverflowAttack.sln bots/stackoverflowattack/
COPY bots/stackoverflowattack/src/StackOverflowAttack/StackOverflowAttack.csproj bots/stackoverflowattack/src/StackOverflowAttack/
COPY bots/stackoverflowattack/tests/StackOverflowAttack.Tests/StackOverflowAttack.Tests.csproj bots/stackoverflowattack/tests/StackOverflowAttack.Tests/

# Restore dependencies
WORKDIR /src/bots/stackoverflowattack
RUN dotnet restore

# Copy common project source
WORKDIR /src
COPY common/BattleshipsBot.Common/ common/BattleshipsBot.Common/

# Copy bot source code
COPY bots/stackoverflowattack/src/ bots/stackoverflowattack/src/
COPY bots/stackoverflowattack/tests/ bots/stackoverflowattack/tests/

# Run tests
WORKDIR /src/bots/stackoverflowattack
RUN dotnet test tests/StackOverflowAttack.Tests/StackOverflowAttack.Tests.csproj --configuration Release

# Build and publish
RUN dotnet publish src/StackOverflowAttack/StackOverflowAttack.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "StackOverflowAttack.dll"]
