using System.Text.Json.Serialization;

namespace SharpShooter.Skirmish.Messages;

public class RegisterPayload
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}
