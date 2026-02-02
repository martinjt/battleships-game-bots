FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY StackOverflowAttack.sln .
COPY src/StackOverflowAttack/StackOverflowAttack.csproj src/StackOverflowAttack/
COPY tests/StackOverflowAttack.Tests/StackOverflowAttack.Tests.csproj tests/StackOverflowAttack.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/
COPY tests/ tests/

# Run tests
RUN dotnet test tests/StackOverflowAttack.Tests/StackOverflowAttack.Tests.csproj --configuration Release

# Build and publish
RUN dotnet publish src/StackOverflowAttack/StackOverflowAttack.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "StackOverflowAttack.dll"]
