using System.Text.Json.Serialization;

namespace HeatSeeker.Tournament.Models;

public class PlayerRegistrationRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("maxConcurrentGames")]
    public int MaxConcurrentGames { get; set; } = 5;
}

public class PlayerRegistrationResponse
{
    [JsonPropertyName("player")]
    public PlayerInfo Player { get; set; } = new();
}

public class PlayerInfo
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("authSecret")]
    public string? AuthSecret { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("maxConcurrentGames")]
    public int MaxConcurrentGames { get; set; }
}

public class TournamentJoinRequest
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}
