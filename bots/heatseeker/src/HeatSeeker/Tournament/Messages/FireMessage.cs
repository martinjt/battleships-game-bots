using System.Text.Json.Serialization;

namespace HeatSeeker.Tournament.Messages;

public class FireRequestPayload
{
    [JsonPropertyName("request")]
    public FireRequest Request { get; set; } = new();
}

public class FireRequest
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;
}

public class FireResponsePayload
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public FireResponseData Response { get; set; } = new();
}

public class FireResponseData
{
    [JsonPropertyName("target")]
    public Position Target { get; set; } = new();
}
