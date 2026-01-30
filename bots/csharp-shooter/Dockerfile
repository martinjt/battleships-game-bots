FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY SharpShooter.sln .
COPY src/SharpShooter/SharpShooter.csproj src/SharpShooter/
COPY tests/SharpShooter.Tests/SharpShooter.Tests.csproj tests/SharpShooter.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/
COPY tests/ tests/

# Run tests
RUN dotnet test tests/SharpShooter.Tests/SharpShooter.Tests.csproj --configuration Release --no-restore

# Build and publish
RUN dotnet publish src/SharpShooter/SharpShooter.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SharpShooter.dll"]
