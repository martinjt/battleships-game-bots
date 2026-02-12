using System.Text.Json.Serialization;

namespace DiagonalDominion.Skirmish.Messages;

public class RegisterPayload
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}
