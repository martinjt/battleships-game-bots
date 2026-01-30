namespace SharpShooter;

public enum Orientation
{
    Horizontal,
    Vertical
}

public record Ship(string Name, int Length, Coordinate StartPosition, Orientation Orientation);

public interface IShipPlacer
{
    List<Ship> PlaceShips();
}

public class RandomShipPlacer : IShipPlacer
{
    private const int BoardSize = 10;
    private readonly Random _random = new();

    // Standard battleship fleet
    private readonly Dictionary<string, int> _fleet = new()
    {
        { "Carrier", 5 },
        { "Battleship", 4 },
        { "Cruiser", 3 },
        { "Submarine", 3 },
        { "Destroyer", 2 }
    };

    public List<Ship> PlaceShips()
    {
        var placedShips = new List<Ship>();
        var occupiedCells = new HashSet<Coordinate>();

        foreach (var (name, length) in _fleet)
        {
            Ship? ship = null;
            var attempts = 0;
            const int maxAttempts = 100;

            while (ship == null && attempts < maxAttempts)
            {
                attempts++;
                var orientation = _random.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
                var startX = orientation == Orientation.Horizontal
                    ? _random.Next(0, BoardSize - length + 1)
                    : _random.Next(0, BoardSize);
                var startY = orientation == Orientation.Vertical
                    ? _random.Next(0, BoardSize - length + 1)
                    : _random.Next(0, BoardSize);

                var startPos = new Coordinate(startX, startY);
                var cells = GetShipCells(startPos, length, orientation);

                // Check if any cells are already occupied
                if (!cells.Any(c => occupiedCells.Contains(c)))
                {
                    ship = new Ship(name, length, startPos, orientation);
                    foreach (var cell in cells)
                    {
                        occupiedCells.Add(cell);
                    }
                }
            }

            if (ship == null)
            {
                throw new InvalidOperationException($"Failed to place {name} after {maxAttempts} attempts");
            }

            placedShips.Add(ship);
        }

        return placedShips;
    }

    private static List<Coordinate> GetShipCells(Coordinate start, int length, Orientation orientation)
    {
        var cells = new List<Coordinate>();
        for (int i = 0; i < length; i++)
        {
            var coord = orientation == Orientation.Horizontal
                ? new Coordinate(start.X + i, start.Y)
                : new Coordinate(start.X, start.Y + i);
            cells.Add(coord);
        }
        return cells;
    }
}
