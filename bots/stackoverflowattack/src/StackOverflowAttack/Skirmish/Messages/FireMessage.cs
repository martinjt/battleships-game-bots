using System.Text.Json.Serialization;

namespace StackOverflowAttack.Skirmish.Messages;

public class FireRequestPayload
{
    [JsonPropertyName("request")]
    public FireRequest Request { get; set; } = new();
}

public class FireRequest
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("opponentView")]
    public OpponentView? OpponentView { get; set; }

    [JsonPropertyName("lastShot")]
    public LastShot? LastShot { get; set; }
}

public class OpponentView
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("shots")]
    public List<ShotResult> Shots { get; set; } = new();
}

public class ShotResult
{
    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("col")]
    public int Col { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}

public class LastShot
{
    [JsonPropertyName("shooterPlayerId")]
    public string? ShooterPlayerId { get; set; }

    [JsonPropertyName("target")]
    public Position? Target { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("sunkShipTypeId")]
    public string? SunkShipTypeId { get; set; }
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
