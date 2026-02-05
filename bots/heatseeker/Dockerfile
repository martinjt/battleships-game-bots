FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HeatSeeker.sln .
COPY src/HeatSeeker/HeatSeeker.csproj src/HeatSeeker/
COPY tests/HeatSeeker.Tests/HeatSeeker.Tests.csproj tests/HeatSeeker.Tests/

RUN dotnet restore

COPY src/ src/
COPY tests/ tests/

RUN dotnet test tests/HeatSeeker.Tests/HeatSeeker.Tests.csproj --configuration Release

RUN dotnet publish src/HeatSeeker/HeatSeeker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HeatSeeker.dll"]
