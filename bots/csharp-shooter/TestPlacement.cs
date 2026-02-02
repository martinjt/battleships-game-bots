using System;
using System.Linq;
using System.Text.Json;
using SharpShooter;
using SharpShooter.Tournament.Messages;

class TestPlacement
{
    static void Main()
    {
        var shipPlacer = new RandomShipPlacer();
        var ships = shipPlacer.PlaceShips();

        Console.WriteLine($"Ships placed: {ships.Count}");
        foreach (var ship in ships)
        {
            Console.WriteLine($"  {ship.Name}: ({ship.StartPosition.X}, {ship.StartPosition.Y}) {ship.Orientation}");
        }

        // Convert to API format
        var shipPlacements = ships.Select(ship =>
        {
            var start = ship.StartPosition;
            var orientation = ship.Orientation == Orientation.Horizontal ? "H" : "V";
            return new ShipPlacement
            {
                TypeId = ship.Name.ToUpperInvariant(),
                Start = new Position { Col = start.X, Row = start.Y },
                Orientation = orientation
            };
        }).ToList();

        var response = new PlaceShipsResponsePayload
        {
            GameId = "test-game-123",
            Response = new PlaceShipsResponseData
            {
                Placements = shipPlacements
            }
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        Console.WriteLine("\nJSON Output:");
        Console.WriteLine(json);

        // Now test with the WebSocket message wrapper
        var wsMessage = new WebSocketMessage<PlaceShipsResponsePayload>
        {
            MessageType = MessageTypes.PlaceShipsResponse,
            Payload = response
        };

        var wsJson = JsonSerializer.Serialize(wsMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        Console.WriteLine("\nWebSocket Message JSON:");
        Console.WriteLine(wsJson);
    }
}
