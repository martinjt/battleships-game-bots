# CSharp Shooter 🎯

A .NET/C# battleships bot with a simple left-to-right firing pattern and random ship placement.

## Strategy

- **Firing Pattern**: Systematic left-to-right, top-to-bottom sweep
- **Ship Placement**: Randomly positioned ships without overlap

## Skirmish Mode

The bot supports WebSocket-based skirmish mode for the live Battleships API.

### Environment Variables

- `SKIRMISH_MODE=true` - Enable skirmish mode (required)
- `BOT_NAME=csharp-shooter` - Your bot's display name (default: csharp-shooter)
- `GAME_API_URL=https://battleships.devrel.hny.wtf` - API base URL (default shown)
- `SKIRMISH_ID=<uuid>` - Optional: Specific skirmish to join

### Running in Skirmish Mode

```bash
# Local development
cd src/SharpShooter
SKIRMISH_MODE=true \
BOT_NAME=my-bot \
dotnet run

# Docker
docker build -t csharp-shooter .
docker run \
  -e SKIRMISH_MODE=true \
  -e BOT_NAME=my-bot \
  -e GAME_API_URL=https://battleships.devrel.hny.wtf \
  csharp-shooter
```

### How Skirmish Mode Works

1. **HTTP Registration**: Bot registers with `/api/v1/players` to get a player ID
2. **WebSocket Connection**: Connects to `wss://battleships.devrel.hny.wtf/ws/player`
3. **Game Loop**: Responds to server messages:
   - `PLACE_SHIPS_REQUEST` → Uses RandomShipPlacer
   - `FIRE_REQUEST` → Uses LeftToRightFiringStrategy
   - `GAME_UPDATE` → Logs game status
   - `ERROR` → Logs errors
4. **Auto-reconnection**: Exponential backoff (1s, 2s, 4s, 8s, 16s), max 5 attempts

## Building

```bash
# Build and run tests
dotnet build
dotnet test

# Build Docker image
docker build -t csharp-shooter .

# Run locally (legacy mode)
docker run -e GAME_API_URL=https://battleships.devrel.hny.wtf csharp-shooter
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReporter=lcov

# Run specific test
dotnet test --filter "FullyQualifiedName~LeftToRightFiringStrategyTests"
```

## Project Structure

```
csharp-shooter/
├── src/SharpShooter/
│   ├── Program.cs                      # Entry point (skirmish/legacy mode)
│   ├── BattleshipsBot.cs               # Legacy bot logic
│   ├── FiringStrategy.cs               # Left-to-right firing pattern
│   ├── ShipPlacer.cs                   # Random ship placement
│   └── Skirmish/
│       ├── TournamentClient.cs         # Skirmish orchestration
│       ├── TournamentWebSocketClient.cs # WebSocket management
│       ├── Messages/                   # WebSocket message DTOs
│       │   ├── WebSocketMessage.cs
│       │   ├── RegisterMessage.cs
│       │   ├── PlaceShipsMessage.cs
│       │   ├── FireMessage.cs
│       │   └── GameUpdateMessage.cs
│       └── Models/
│           ├── PlayerRegistration.cs   # HTTP API models
│           └── TournamentConfig.cs     # Configuration
└── tests/SharpShooter.Tests/
    ├── FiringStrategyTests.cs          # Firing pattern tests
    └── ShipPlacerTests.cs              # Ship placement tests
```

## Features

- WebSocket-based skirmish mode with auto-reconnection
- Comprehensive unit tests with xUnit and FluentAssertions
- Structured logging with Microsoft.Extensions.Logging
- HTTP client for player registration
- Clean separation of concerns (firing strategy, ship placement)
- Multi-stage Docker build with test execution
- Graceful shutdown handling (Ctrl+C)
- .NET 9.0 runtime
