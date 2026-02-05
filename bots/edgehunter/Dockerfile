FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY EdgeHunter.sln .
COPY src/EdgeHunter/EdgeHunter.csproj src/EdgeHunter/
COPY tests/EdgeHunter.Tests/EdgeHunter.Tests.csproj tests/EdgeHunter.Tests/

RUN dotnet restore

COPY src/ src/
COPY tests/ tests/

RUN dotnet test tests/EdgeHunter.Tests/EdgeHunter.Tests.csproj --configuration Release

RUN dotnet publish src/EdgeHunter/EdgeHunter.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EdgeHunter.dll"]
