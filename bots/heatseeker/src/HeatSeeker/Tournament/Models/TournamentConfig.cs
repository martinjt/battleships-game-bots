namespace HeatSeeker.Tournament.Models;

public class TournamentConfig
{
    public string ApiUrl { get; set; } = "https://battleships.devrel.hny.wtf";
    public string BotName { get; set; } = "SunkCost";
    public string? TournamentId { get; set; }
    public int MaxReconnectAttempts { get; set; } = 5;
    public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public string GetWebSocketUrl(string playerId)
    {
        var wsBaseUrl = ApiUrl.Replace("https://", "wss://").Replace("http://", "ws://");
        return $"{wsBaseUrl}/ws/player/{playerId}";
    }

    public static TournamentConfig FromEnvironment()
    {
        return new TournamentConfig
        {
            ApiUrl = Environment.GetEnvironmentVariable("GAME_API_URL") ?? "https://battleships.devrel.hny.wtf",
            BotName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "SunkCost",
            TournamentId = Environment.GetEnvironmentVariable("TOURNAMENT_ID")
        };
    }
}
