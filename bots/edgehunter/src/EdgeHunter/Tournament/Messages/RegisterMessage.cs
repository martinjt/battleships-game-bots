using System.Text.Json.Serialization;

namespace EdgeHunter.Tournament.Messages;

public class RegisterPayload
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}
