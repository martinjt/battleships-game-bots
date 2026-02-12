using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using HeatSeeker.Skirmish.Messages;
using HeatSeeker.Skirmish.Models;

namespace HeatSeeker.Skirmish;

public class SkirmishClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private SkirmishWebSocketClient? _webSocketClient;
    private readonly ILogger _logger;
    private readonly SkirmishConfig _config;
    private readonly IShipPlacer _shipPlacer;
    private readonly Dictionary<string, IFiringStrategy> _gameStrategies = new();
    private string? _playerId;
    private string? _authSecret;
    private string? _currentGameId;
    private bool _disposed;

    public SkirmishClient(SkirmishConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient { BaseAddress = new Uri(config.ApiUrl) };
        _shipPlacer = new DispersedAsymmetricShipPlacer();
    }

    private IFiringStrategy GetOrCreateStrategy(string gameId)
    {
        if (!_gameStrategies.TryGetValue(gameId, out var strategy))
        {
            strategy = new HeatMapFiringStrategy();
            _gameStrategies[gameId] = strategy;
            _logger.LogInformation("Created new firing strategy for game: {GameId}", gameId);
        }
        return strategy;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existingCredentials = await PlayerCredentials.LoadAsync(_config.BotName).ConfigureAwait(false);
            if (existingCredentials != null)
            {
                _playerId = existingCredentials.PlayerId;
                _authSecret = existingCredentials.AuthSecret;
                _logger.LogInformation("Loaded existing credentials for: {BotName} (Player ID: {PlayerId})",
                    _config.BotName, _playerId);
            }
            else
            {
                _logger.LogInformation("Registering new player: {BotName}", _config.BotName);
                await RegisterPlayerAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(_config.SkirmishId))
            {
                _logger.LogInformation("Joining tournament: {SkirmishId}", _config.SkirmishId);
                await JoinSkirmishAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Connecting to WebSocket...");
            var wsUrl = _config.GetWebSocketUrl(_playerId!);
            _webSocketClient = new SkirmishWebSocketClient(wsUrl, _config.MaxReconnectAttempts, _logger);
            _webSocketClient.MessageReceived += HandleMessageAsync;
            _webSocketClient.Connected += OnConnectedAsync;
            _webSocketClient.Disconnected += OnDisconnectedAsync;
            await _webSocketClient.ConnectAsync(cancellationToken).ConfigureAwait(false);

            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Skirmish client cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in skirmish client");
            throw;
        }
    }

    private async Task RegisterPlayerAsync(CancellationToken cancellationToken)
    {
        var request = new PlayerRegistrationRequest { Name = _config.BotName };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/players", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var registration = await response.Content.ReadFromJsonAsync<PlayerRegistrationResponse>(cancellationToken).ConfigureAwait(false);
        if (registration == null || registration.Player == null || string.IsNullOrEmpty(registration.Player.PlayerId))
        {
            throw new InvalidOperationException("Failed to register player: Invalid response");
        }

        _playerId = registration.Player.PlayerId;
        _authSecret = registration.Player.AuthSecret;

        _logger.LogInformation("Registered player: {PlayerId}", _playerId);

        var credentials = new PlayerCredentials
        {
            PlayerId = _playerId,
            AuthSecret = _authSecret,
            BotName = _config.BotName
        };
        await credentials.SaveAsync().ConfigureAwait(false);
        _logger.LogInformation("Saved player credentials to {Path}", PlayerCredentials.GetCredentialsPath());
    }

    private async Task JoinSkirmishAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_playerId))
        {
            throw new InvalidOperationException("Cannot join tournament: Player not registered");
        }

        var request = new SkirmishJoinRequest { PlayerId = _playerId };
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v1/skirmishes/{_config.SkirmishId}/players",
            request,
            cancellationToken
        ).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Joined tournament successfully");
    }

    private Task OnConnectedAsync()
    {
        _logger.LogInformation("WebSocket connected, waiting for game requests...");
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync()
    {
        _logger.LogWarning("WebSocket disconnected");
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(string messageType, JsonElement payload)
    {
        try
        {
            using var cts = new CancellationTokenSource(_config.MessageTimeout);

            switch (messageType)
            {
                case MessageTypes.Registered:
                    _logger.LogInformation("Successfully registered with WebSocket");
                    break;

                case MessageTypes.PlaceShipsRequest:
                    await HandlePlaceShipsRequestAsync(payload, cts.Token).ConfigureAwait(false);
                    break;

                case MessageTypes.FireRequest:
                    await HandleFireRequestAsync(payload, cts.Token).ConfigureAwait(false);
                    break;

                case MessageTypes.GameUpdate:
                    await HandleGameUpdateAsync(payload).ConfigureAwait(false);
                    break;

                case MessageTypes.Error:
                    HandleErrorMessage(payload);
                    break;

                default:
                    _logger.LogWarning("Received unknown message type: {MessageType}", messageType);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Message handling timed out for message type: {MessageType}", messageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message type: {MessageType}", messageType);
        }
    }

    private async Task HandlePlaceShipsRequestAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<PlaceShipsRequestPayload>(payload);
        if (request == null || request.Request == null)
        {
            _logger.LogError("Failed to deserialize PlaceShipsRequest");
            return;
        }

        _currentGameId = request.Request.GameId;
        _logger.LogInformation("Placing ships for game: {GameId}", _currentGameId);

        var ships = _shipPlacer.PlaceShips();
        _logger.LogInformation("Ships placed: {Count}", ships.Count);

        var shipPlacements = ships.Select(ConvertToShipPlacement).ToList();

        var response = new PlaceShipsResponsePayload
        {
            GameId = request.Request.GameId,
            Response = new PlaceShipsResponseData
            {
                Placements = shipPlacements
            }
        };

        await _webSocketClient!.SendMessageAsync(MessageTypes.PlaceShipsResponse, response, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Ship placement sent");
    }

    private async Task HandleFireRequestAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<FireRequestPayload>(payload);
        if (request == null)
        {
            _logger.LogError("Failed to deserialize FireRequest");
            return;
        }

        var gameId = request.Request.GameId;
        _logger.LogInformation("Firing shot for game: {GameId}", gameId);

        // Get or create per-game strategy
        var strategy = GetOrCreateStrategy(gameId);

        // Update strategy with game state from opponent view
        if (request.Request.OpponentView?.Shots != null)
        {
            _logger.LogInformation("Processing {Count} previous shots", request.Request.OpponentView.Shots.Count);
            foreach (var previousShot in request.Request.OpponentView.Shots)
            {
                var coord = new Coordinate(previousShot.Col, previousShot.Row);
                if (previousShot.Result.Equals("Hit", StringComparison.OrdinalIgnoreCase))
                {
                    strategy.RecordHit(coord);
                }
                else if (previousShot.Result.Equals("Miss", StringComparison.OrdinalIgnoreCase))
                {
                    strategy.RecordMiss(coord);
                }
            }
        }

        // Check if last shot resulted in a sunk ship
        if (request.Request.LastShot?.SunkShipTypeId != null)
        {
            _logger.LogInformation("Ship sunk: {ShipTypeId}", request.Request.LastShot.SunkShipTypeId);
            strategy.RecordSunk();
        }

        var shot = strategy.GetNextShot();
        _logger.LogInformation("Fired at: ({X}, {Y})", shot.X, shot.Y);

        var response = new FireResponsePayload
        {
            GameId = gameId,
            Response = new FireResponseData
            {
                Target = new Position { Col = shot.X, Row = shot.Y }
            }
        };

        await _webSocketClient!.SendMessageAsync(MessageTypes.FireResponse, response, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Fire response sent");
    }

    private Task HandleGameUpdateAsync(JsonElement payload)
    {
        var update = JsonSerializer.Deserialize<GameUpdatePayload>(payload);
        if (update == null)
        {
            _logger.LogWarning("Failed to deserialize GameUpdate");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Game update - GameId: {GameId}, Status: {Status}, Message: {Message}",
            update.GameId, update.Status, update.Message ?? "none");

        if (!string.IsNullOrEmpty(update.Winner))
        {
            _logger.LogInformation("Game finished - Winner: {Winner}", update.Winner);
            // Clean up strategy for finished game
            if (!string.IsNullOrEmpty(update.GameId) && _gameStrategies.Remove(update.GameId))
            {
                _logger.LogInformation("Removed strategy for finished game: {GameId}", update.GameId);
            }
        }

        return Task.CompletedTask;
    }

    private void HandleErrorMessage(JsonElement payload)
    {
        var error = JsonSerializer.Deserialize<ErrorPayload>(payload);
        if (error != null)
        {
            _logger.LogError("Server error: {Error} - {Message}", error.Error, error.Message);
        }
    }

    private static ShipPlacement ConvertToShipPlacement(Ship ship)
    {
        var start = ship.StartPosition;
        var orientation = ship.Orientation == Orientation.Horizontal ? "H" : "V";

        return new ShipPlacement
        {
            TypeId = ship.Name.ToUpperInvariant(),
            Start = new Position { Col = start.X, Row = start.Y },
            Orientation = orientation
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _webSocketClient?.Dispose();
        _httpClient.Dispose();
    }
}
