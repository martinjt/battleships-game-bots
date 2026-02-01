using System.Text.Json.Serialization;

namespace StackOverflowAttack.Tournament.Messages;

public class PlaceShipsRequestPayload
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;
}

public class PlaceShipsResponsePayload
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("ships")]
    public List<ShipPlacement> Ships { get; set; } = new();
}

public class ShipPlacement
{
    [JsonPropertyName("shipType")]
    public string ShipType { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public Position Start { get; set; } = new();

    [JsonPropertyName("end")]
    public Position End { get; set; } = new();
}

public class Position
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}
