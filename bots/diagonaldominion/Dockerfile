FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DiagonalDominion.sln .
COPY src/DiagonalDominion/DiagonalDominion.csproj src/DiagonalDominion/
COPY tests/DiagonalDominion.Tests/DiagonalDominion.Tests.csproj tests/DiagonalDominion.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/
COPY tests/ tests/

# Run tests
RUN dotnet test tests/DiagonalDominion.Tests/DiagonalDominion.Tests.csproj --configuration Release

# Build and publish
RUN dotnet publish src/DiagonalDominion/DiagonalDominion.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DiagonalDominion.dll"]
