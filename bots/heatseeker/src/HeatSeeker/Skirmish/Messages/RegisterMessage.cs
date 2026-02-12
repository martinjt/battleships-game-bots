using System.Text.Json.Serialization;

namespace HeatSeeker.Skirmish.Messages;

public class RegisterPayload
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}
