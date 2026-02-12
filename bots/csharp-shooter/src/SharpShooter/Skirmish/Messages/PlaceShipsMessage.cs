using System.Text.Json.Serialization;

namespace SharpShooter.Skirmish.Messages;

public class PlaceShipsRequestPayload
{
    [JsonPropertyName("request")]
    public PlaceShipsRequestData Request { get; set; } = new();
}

public class PlaceShipsRequestData
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}

public class PlaceShipsResponsePayload
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public PlaceShipsResponseData Response { get; set; } = new();
}

public class PlaceShipsResponseData
{
    [JsonPropertyName("placements")]
    public List<ShipPlacement> Placements { get; set; } = new();
}

public class ShipPlacement
{
    [JsonPropertyName("typeId")]
    public string TypeId { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public Position Start { get; set; } = new();

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = string.Empty;
}

public class Position
{
    [JsonPropertyName("col")]
    public int Col { get; set; }

    [JsonPropertyName("row")]
    public int Row { get; set; }
}
