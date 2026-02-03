namespace SharpShooter;

/// <summary>
/// Adaptive ship placer that counters opponent strategies
/// </summary>
public class AdaptiveShipPlacer : IShipPlacer
{
    private const int BoardSize = 10;
    private readonly Random _random = new();
    private readonly OpponentDetector _opponentDetector;

    private readonly Dictionary<string, int> _fleet = new()
    {
        { "Carrier", 5 },
        { "Battleship", 4 },
        { "Cruiser", 3 },
        { "Submarine", 3 },
        { "Destroyer", 2 }
    };

    public AdaptiveShipPlacer(OpponentDetector opponentDetector)
    {
        _opponentDetector = opponentDetector;
    }

    public List<Ship> PlaceShips()
    {
        var strategy = _opponentDetector.GetDetectedStrategy();

        return strategy switch
        {
            OpponentStrategy.CenterFirst => PlaceShipsAntiCenterFirst(),
            OpponentStrategy.CornerCheckerboard => PlaceShipsAntiCheckerboard(),
            _ => PlaceShipsBalanced()
        };
    }

    /// <summary>
    /// Counter DepthCharge's center-first strategy
    /// Place ships on edges and corners, away from center
    /// </summary>
    private List<Ship> PlaceShipsAntiCenterFirst()
    {
        var placedShips = new List<Ship>();
        var occupiedCells = new HashSet<Coordinate>();

        foreach (var (name, length) in _fleet)
        {
            Ship? ship = null;
            var attempts = 0;
            const int maxAttempts = 200;

            while (ship == null && attempts < maxAttempts)
            {
                attempts++;

                // Prefer edges: top row, bottom row, left column, right column
                var edge = _random.Next(4);
                Coordinate startPos;
                Orientation orientation;

                switch (edge)
                {
                    case 0: // Top edge (row 0)
                        orientation = Orientation.Horizontal;
                        startPos = new Coordinate(_random.Next(0, BoardSize - length + 1), 0);
                        break;
                    case 1: // Bottom edge (row 9)
                        orientation = Orientation.Horizontal;
                        startPos = new Coordinate(_random.Next(0, BoardSize - length + 1), 9);
                        break;
                    case 2: // Left edge (column 0)
                        orientation = Orientation.Vertical;
                        startPos = new Coordinate(0, _random.Next(0, BoardSize - length + 1));
                        break;
                    default: // Right edge (column 9)
                        orientation = Orientation.Vertical;
                        startPos = new Coordinate(9, _random.Next(0, BoardSize - length + 1));
                        break;
                }

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
                // Fallback to random placement if edge placement fails
                ship = PlaceShipRandom(name, length, occupiedCells);
            }

            placedShips.Add(ship);
        }

        return placedShips;
    }

    /// <summary>
    /// Counter Mirage's checkerboard strategy
    /// Place ships vertically on odd columns to exploit checkerboard gaps
    /// </summary>
    private List<Ship> PlaceShipsAntiCheckerboard()
    {
        var placedShips = new List<Ship>();
        var occupiedCells = new HashSet<Coordinate>();

        // Odd columns: 1, 3, 5, 7, 9
        var oddColumns = new[] { 1, 3, 5, 7, 9 };

        foreach (var (name, length) in _fleet)
        {
            Ship? ship = null;
            var attempts = 0;
            const int maxAttempts = 200;

            while (ship == null && attempts < maxAttempts)
            {
                attempts++;

                // Try to place vertically on odd columns
                if (attempts < 100)
                {
                    var col = oddColumns[_random.Next(oddColumns.Length)];
                    var row = _random.Next(0, BoardSize - length + 1);
                    var startPos = new Coordinate(col, row);
                    var cells = GetShipCells(startPos, length, Orientation.Vertical);

                    if (!cells.Any(c => occupiedCells.Contains(c)))
                    {
                        ship = new Ship(name, length, startPos, Orientation.Vertical);
                        foreach (var cell in cells)
                        {
                            occupiedCells.Add(cell);
                        }
                    }
                }
                else
                {
                    // Fallback: horizontal on odd rows
                    var oddRows = new[] { 1, 3, 5, 7, 9 };
                    var row = oddRows[_random.Next(oddRows.Length)];
                    var col = _random.Next(0, BoardSize - length + 1);
                    var startPos = new Coordinate(col, row);
                    var cells = GetShipCells(startPos, length, Orientation.Horizontal);

                    if (!cells.Any(c => occupiedCells.Contains(c)))
                    {
                        ship = new Ship(name, length, startPos, Orientation.Horizontal);
                        foreach (var cell in cells)
                        {
                            occupiedCells.Add(cell);
                        }
                    }
                }
            }

            if (ship == null)
            {
                // Final fallback
                ship = PlaceShipRandom(name, length, occupiedCells);
            }

            placedShips.Add(ship);
        }

        return placedShips;
    }

    /// <summary>
    /// Balanced placement for unknown opponents
    /// Mix of edge and interior placement
    /// </summary>
    private List<Ship> PlaceShipsBalanced()
    {
        var placedShips = new List<Ship>();
        var occupiedCells = new HashSet<Coordinate>();

        foreach (var (name, length) in _fleet)
        {
            var ship = PlaceShipRandom(name, length, occupiedCells);
            placedShips.Add(ship);
        }

        return placedShips;
    }

    private Ship PlaceShipRandom(string name, int length, HashSet<Coordinate> occupiedCells)
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

        return ship;
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
