namespace EdgeHunter.Skirmish.Models;

public class SkirmishConfig
{
    public string ApiUrl { get; set; } = "https://battleships.devrel.hny.wtf";
    public string BotName { get; set; } = "NavalGazing";
    public string? SkirmishId { get; set; }
    public int MaxReconnectAttempts { get; set; } = 5;
    public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public string GetWebSocketUrl(string playerId)
    {
        var wsBaseUrl = ApiUrl.Replace("https://", "wss://").Replace("http://", "ws://");
        return $"{wsBaseUrl}/ws/player/{playerId}";
    }

    public static SkirmishConfig FromEnvironment()
    {
        return new SkirmishConfig
        {
            ApiUrl = Environment.GetEnvironmentVariable("GAME_API_URL") ?? "https://battleships.devrel.hny.wtf",
            BotName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "NavalGazing",
            SkirmishId = Environment.GetEnvironmentVariable("SKIRMISH_ID")
        };
    }
}
