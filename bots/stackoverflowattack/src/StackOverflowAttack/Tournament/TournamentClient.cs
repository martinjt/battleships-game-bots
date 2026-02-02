using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackOverflowAttack.Strategies;
using StackOverflowAttack.Tournament.Messages;
using StackOverflowAttack.Tournament.Models;

namespace StackOverflowAttack.Tournament;

public class TournamentClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private TournamentWebSocketClient? _webSocketClient;
    private readonly ILogger _logger;
    private readonly TournamentConfig _config;
    private readonly IShipPlacer _shipPlacer;
    private readonly IFiringStrategy _firingStrategy;
    private string? _playerId;
    private string? _authSecret;
    private string? _currentGameId;
    private bool _disposed;

    public TournamentClient(TournamentConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient { BaseAddress = new Uri(config.ApiUrl) };
        _shipPlacer = new RandomShipPlacer();
        _firingStrategy = new ProbabilityDensityFiringStrategy();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Try to load existing credentials
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
                // Step 2: Register player via HTTP
                _logger.LogInformation("Registering new player: {BotName}", _config.BotName);
                await RegisterPlayerAsync(cancellationToken).ConfigureAwait(false);
            }

            // Step 3: Join tournament if specified
            if (!string.IsNullOrEmpty(_config.TournamentId))
            {
                _logger.LogInformation("Joining tournament: {TournamentId}", _config.TournamentId);
                await JoinTournamentAsync(cancellationToken).ConfigureAwait(false);
            }

            // Step 4: Create and connect WebSocket with player ID
            _logger.LogInformation("Connecting to WebSocket...");
            var wsUrl = _config.GetWebSocketUrl(_playerId!);
            _webSocketClient = new TournamentWebSocketClient(wsUrl, _config.MaxReconnectAttempts, _logger);
            _webSocketClient.MessageReceived += HandleMessageAsync;
            _webSocketClient.Connected += OnConnectedAsync;
            _webSocketClient.Disconnected += OnDisconnectedAsync;
            await _webSocketClient.ConnectAsync(cancellationToken).ConfigureAwait(false);

            // Keep running until cancelled
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Tournament client cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in tournament client");
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

        if (!string.IsNullOrEmpty(_authSecret))
        {
            _logger.LogInformation("Received authSecret for secure API updates");
        }

        // Save credentials for reuse
        var credentials = new PlayerCredentials
        {
            PlayerId = _playerId,
            AuthSecret = _authSecret,
            BotName = _config.BotName
        };
        await credentials.SaveAsync().ConfigureAwait(false);
        _logger.LogInformation("Saved player credentials to {Path}", PlayerCredentials.GetCredentialsPath());
    }

    private async Task JoinTournamentAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_playerId))
        {
            throw new InvalidOperationException("Cannot join tournament: Player not registered");
        }

        var request = new TournamentJoinRequest { PlayerId = _playerId };
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v1/tournaments/{_config.TournamentId}/players",
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

        // Use ship placer to generate placement
        var ships = _shipPlacer.PlaceShips();
        _logger.LogInformation("Ships placed: {Count}", ships.Count);

        // Convert to API format
        var shipPlacements = ships.Select(ConvertToShipPlacement).ToList();
        _logger.LogInformation("Converted {Count} ships to placements", shipPlacements.Count);
        foreach (var placement in shipPlacements)
        {
            _logger.LogInformation("  Ship: {TypeId} at ({Col},{Row}) facing {Orientation}",
                placement.TypeId, placement.Start.Col, placement.Start.Row, placement.Orientation);
        }

        // Send response
        var response = new PlaceShipsResponsePayload
        {
            GameId = request.Request.GameId,
            Response = new PlaceShipsResponseData
            {
                Placements = shipPlacements
            }
        };

        // Debug: Log the JSON being sent
        var debugJson = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        _logger.LogInformation("Sending placement response JSON:\n{Json}", debugJson);

        await _webSocketClient.SendMessageAsync(MessageTypes.PlaceShipsResponse, response, cancellationToken).ConfigureAwait(false);
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

        _logger.LogInformation("Firing shot for game: {GameId}", request.GameId);

        // Use firing strategy to get next shot
        var shot = _firingStrategy.GetNextShot();
        _logger.LogInformation("Fired at: ({X}, {Y})", shot.X, shot.Y);

        // Send response
        var response = new FireResponsePayload
        {
            GameId = request.GameId,
            Response = new FireResponseData
            {
                Target = new Position { Col = shot.X, Row = shot.Y }
            }
        };

        await _webSocketClient.SendMessageAsync(MessageTypes.FireResponse, response, cancellationToken).ConfigureAwait(false);
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
        }

        // Reset firing strategy for new game
        if (update.Status == "started" || update.Status == "STARTED")
        {
            _logger.LogInformation("New game started, resetting firing strategy");
            _firingStrategy.Reset();
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
        else
        {
            _logger.LogError("Received error message but failed to deserialize");
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _webSocketClient?.Dispose();
        _httpClient.Dispose();
    }
}
