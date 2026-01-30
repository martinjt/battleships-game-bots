using System.Text.Json.Serialization;

namespace SharpShooter.Tournament.Messages;

public class RegisterPayload
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}
