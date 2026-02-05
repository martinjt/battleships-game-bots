using System.Text.Json.Serialization;

namespace HeatSeeker.Tournament.Messages;

public class RegisterPayload
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}
