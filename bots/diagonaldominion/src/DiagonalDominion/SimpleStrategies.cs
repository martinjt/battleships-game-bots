using BattleshipsBot.Common.Interfaces;

namespace DiagonalDominion;

/// <summary>
/// Simple left-to-right firing strategy
/// </summary>
public class SimpleFiringStrategy : IFiringStrategy
{
    private int _currentX = 0;
    private int _currentY = 0;
    private const int BoardSize = 10;

    public Coordinate GetNextShot()
    {
        var shot = new Coordinate(_currentX, _currentY);

        // Move left to right, top to bottom
        _currentX++;
        if (_currentX >= BoardSize)
        {
            _currentX = 0;
            _currentY++;
            if (_currentY >= BoardSize)
            {
                _currentY = 0; // Wrap around to start
            }
        }

        return shot;
    }

    public void RecordHit(Coordinate coordinate) { }
    public void RecordMiss(Coordinate coordinate) { }
    public void RecordSunk() { }

    public void Reset()
    {
        _currentX = 0;
        _currentY = 0;
    }
}

/// <summary>
/// Simple random ship placer
/// </summary>
public class SimpleShipPlacer : IShipPlacer
{
    private const int BoardSize = 10;
    private readonly Random _random = new();

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

/// <summary>
/// Factory for simple firing strategy
/// </summary>
public class SimpleFiringStrategyFactory : IFiringStrategyFactory
{
    public IFiringStrategy CreateStrategy() => new SimpleFiringStrategy();
}
