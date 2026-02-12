# Skirmish Mode Quick Start

## Prerequisites

- .NET 9.0 SDK installed
- Access to https://battleships.devrel.hny.wtf

## Local Testing

### Step 1: Build and Test

```bash
cd /home/martin/repos/battleships-game-bots/bots/csharp-shooter
dotnet build
dotnet test
```

Expected output: `Total tests: 14, Passed: 14`

### Step 2: Run in Skirmish Mode

```bash
cd src/SharpShooter

# Basic skirmish mode
SKIRMISH_MODE=true \
BOT_NAME=csharp-shooter-test \
GAME_API_URL=https://battleships.devrel.hny.wtf \
dotnet run

# With specific skirmish ID
SKIRMISH_MODE=true \
BOT_NAME=csharp-shooter-test \
GAME_API_URL=https://battleships.devrel.hny.wtf \
SKIRMISH_ID=<your-skirmish-id> \
dotnet run
```

### Expected Log Output

```
info: TournamentClient[0]
      Starting in TOURNAMENT MODE
info: TournamentClient[0]
      Bot Name: csharp-shooter-test
info: TournamentClient[0]
      API URL: https://battleships.devrel.hny.wtf
info: TournamentClient[0]
      Registering player: csharp-shooter-test
info: TournamentClient[0]
      Registered player: <player-id>
info: TournamentClient[0]
      Connecting to WebSocket...
info: TournamentClient[0]
      Connecting to WebSocket (attempt 1/5)...
info: TournamentClient[0]
      WebSocket connected successfully
info: TournamentClient[0]
      Registering with WebSocket...
info: TournamentClient[0]
      Successfully registered with WebSocket
info: TournamentClient[0]
      Placing ships for game: <game-id>
info: TournamentClient[0]
      Ships placed: 5
info: TournamentClient[0]
      Ship placement sent
info: TournamentClient[0]
      Firing shot for game: <game-id>
info: TournamentClient[0]
      Fired at: (0, 0)
...
```

### Step 3: Graceful Shutdown

Press `Ctrl+C` to shutdown gracefully. The bot will disconnect from WebSocket properly.

## Docker Deployment

### Build Image

```bash
cd /home/martin/repos/battleships-game-bots/bots/csharp-shooter
docker build -t csharp-shooter .
```

The Dockerfile runs tests during build, so if build succeeds, tests passed.

### Run Container

```bash
docker run \
  -e SKIRMISH_MODE=true \
  -e BOT_NAME=csharp-shooter \
  -e GAME_API_URL=https://battleships.devrel.hny.wtf \
  csharp-shooter
```

### Run with skirmish ID

```bash
docker run \
  -e SKIRMISH_MODE=true \
  -e BOT_NAME=csharp-shooter \
  -e SKIRMISH_ID=<your-skirmish-id> \
  -e GAME_API_URL=https://battleships.devrel.hny.wtf \
  csharp-shooter
```

## Troubleshooting

### WebSocket Connection Fails

The bot will retry with exponential backoff (1s, 2s, 4s, 8s, 16s) up to 5 times.

Check:
- Network connectivity to battleships.devrel.hny.wtf
- Firewall allows WebSocket connections (wss://)
- API URL is correct

### Player Registration Fails

Check:
- API is accessible: `curl https://battleships.devrel.hny.wtf/api/v1/players`
- BOT_NAME is unique and valid
- GAME_API_URL doesn't have trailing slash

### No Games Starting

The bot is successfully connected but waiting for games. This is normal behavior.
The skirmish organizer needs to start games.

### Message Timeout

If messages take > 30 seconds to process, the bot will log an error.
This shouldn't happen with RandomShipPlacer and LeftToRightFiringStrategy.

## Development Tips

### Enable Debug Logging

Modify `Program.cs` to add debug level:

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
```

### Test Without Skirmish Mode (Legacy)

```bash
cd src/SharpShooter
BOT_NAME=test-bot \
dotnet run
```

This runs the legacy HTTP polling mode (which won't work with real API).

### Check Build Artifacts

```bash
ls -lh src/SharpShooter/bin/Release/net9.0/
```

Should see SharpShooter.dll and dependencies.
