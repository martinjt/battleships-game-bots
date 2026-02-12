using System.Text.Json.Serialization;

namespace BattleshipsBot.Common.Skirmish.Messages;

public class GameUpdatePayload
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ErrorPayload
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
