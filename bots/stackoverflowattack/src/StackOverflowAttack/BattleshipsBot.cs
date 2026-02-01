using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace StackOverflowAttack;

public class BattleshipsBot
{
    private readonly HttpClient _httpClient;
    private readonly string _botName;
    private readonly ILogger _logger;
    private readonly IFiringStrategy _firingStrategy;
    private readonly IShipPlacer _shipPlacer;
    private string? _gameId;

    public BattleshipsBot(string apiUrl, string botName, ILogger logger)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
        _botName = botName;
        _logger = logger;
        _firingStrategy = new LeftToRightFiringStrategy();
        _shipPlacer = new RandomShipPlacer();
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting {BotName}...", _botName);

        while (true)
        {
            try
            {
                if (string.IsNullOrEmpty(_gameId))
                {
                    if (!await JoinGameAsync())
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                }

                await GameLoopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    private async Task<bool> JoinGameAsync()
    {
        try
        {
            // TODO: Replace with actual API endpoint when available
            var response = await _httpClient.PostAsJsonAsync("/api/game/join", new { bot_name = _botName });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JoinGameResponse>();
            _gameId = result?.GameId;

            _logger.LogInformation("Joined game: {GameId}", _gameId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to join game");
            return false;
        }
    }

    private async Task GameLoopAsync()
    {
        var nextShot = _firingStrategy.GetNextShot();

        try
        {
            // TODO: Replace with actual API endpoint when available
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/game/{_gameId}/move",
                new { x = nextShot.X, y = nextShot.Y }
            );

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MoveResponse>();

            _logger.LogInformation("Shot at ({X},{Y}): {Result}", nextShot.X, nextShot.Y, result?.Result ?? "unknown");

            await Task.Delay(TimeSpan.FromSeconds(1)); // Rate limiting
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making move");
        }
    }

    private record JoinGameResponse(string GameId);
    private record MoveResponse(string Result);
}
