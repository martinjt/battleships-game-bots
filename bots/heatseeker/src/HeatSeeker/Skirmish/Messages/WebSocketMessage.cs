using System.Text.Json.Serialization;

namespace HeatSeeker.Skirmish.Messages;

public class WebSocketMessage<T>
{
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public T? Payload { get; set; }
}

public static class MessageTypes
{
    public const string Register = "REGISTER";
    public const string Registered = "REGISTERED";
    public const string PlaceShipsRequest = "PLACE_SHIPS_REQUEST";
    public const string PlaceShipsResponse = "PLACE_SHIPS_RESPONSE";
    public const string FireRequest = "FIRE_REQUEST";
    public const string FireResponse = "FIRE_RESPONSE";
    public const string GameUpdate = "GAME_UPDATE";
    public const string Error = "ERROR";
}
