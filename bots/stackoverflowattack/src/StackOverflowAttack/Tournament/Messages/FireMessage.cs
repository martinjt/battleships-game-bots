using System.Text.Json.Serialization;

namespace StackOverflowAttack.Tournament.Messages;

public class FireRequestPayload
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;
}

public class FireResponsePayload
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public Position Target { get; set; } = new();
}
