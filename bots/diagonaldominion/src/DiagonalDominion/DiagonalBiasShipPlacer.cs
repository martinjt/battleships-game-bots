namespace DiagonalDominion;

/// <summary>
/// Places ships with a diagonal bias pattern to confuse opponents using standard scan patterns.
/// Ships are placed along or near diagonals to be harder to detect with horizontal/vertical sweeps.
/// </summary>
public class DiagonalBiasShipPlacer : IShipPlacer
{
    private const int BoardSize = 10;
    private static readonly Random Random = new();

    private static readonly (string Name, int Length)[] Fleet = new[]
    {
        ("Carrier", 5),
        ("Battleship", 4),
        ("Cruiser", 3),
        ("Submarine", 3),
        ("Destroyer", 2)
    };

    public List<Ship> PlaceShips()
    {
        var ships = new List<Ship>();
        var occupiedCells = new HashSet<Coordinate>();

        foreach (var (name, length) in Fleet)
        {
            var ship = PlaceShipWithDiagonalBias(name, length, occupiedCells);
            ships.Add(ship);
            MarkOccupiedCells(ship, occupiedCells);
        }

        return ships;
    }

    private Ship PlaceShipWithDiagonalBias(string name, int length, HashSet<Coordinate> occupied)
    {
        // Generate candidate positions with diagonal bias
        var candidates = GenerateDiagonalBiasCandidates(name, length, occupied);

        if (candidates.Count > 0)
        {
            return candidates[Random.Next(candidates.Count)];
        }

        // Fallback to any valid position
        return FindAnyValidPlacement(name, length, occupied);
    }

    private static List<Ship> GenerateDiagonalBiasCandidates(string name, int length, HashSet<Coordinate> occupied)
    {
        var candidates = new List<Ship>();

        // Prefer positions where the ship start is on or near a diagonal
        // Diagonals are where x + y is constant (e.g., main diagonal: x + y = 9)

        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                var start = new Coordinate(x, y);
                int diagonalSum = x + y;

                // Check if position is on a "major" diagonal (sum between 4 and 14)
                bool isOnDiagonal = diagonalSum >= 3 && diagonalSum <= 15;

                // Try both orientations
                foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
                {
                    if (CanPlaceShip(start, length, orientation, occupied))
                    {
                        var ship = new Ship(name, length, start, orientation);

                        // Add with higher probability if on diagonal
                        if (isOnDiagonal)
                        {
                            candidates.Add(ship);
                            candidates.Add(ship); // Double weight for diagonal positions
                        }
                        else
                        {
                            candidates.Add(ship);
                        }
                    }
                }
            }
        }

        return candidates;
    }

    private Ship FindAnyValidPlacement(string name, int length, HashSet<Coordinate> occupied)
    {
        // Try random positions until we find one that works
        for (int attempts = 0; attempts < 1000; attempts++)
        {
            var orientation = Random.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
            int maxX = orientation == Orientation.Horizontal ? BoardSize - length : BoardSize - 1;
            int maxY = orientation == Orientation.Vertical ? BoardSize - length : BoardSize - 1;

            var start = new Coordinate(Random.Next(maxX + 1), Random.Next(maxY + 1));

            if (CanPlaceShip(start, length, orientation, occupied))
            {
                return new Ship(name, length, start, orientation);
            }
        }

        throw new InvalidOperationException($"Could not place ship {name} after 1000 attempts");
    }

    private static bool CanPlaceShip(Coordinate start, int length, Orientation orientation, HashSet<Coordinate> occupied)
    {
        for (int i = 0; i < length; i++)
        {
            var cell = orientation == Orientation.Horizontal
                ? new Coordinate(start.X + i, start.Y)
                : new Coordinate(start.X, start.Y + i);

            if (cell.X < 0 || cell.X >= BoardSize || cell.Y < 0 || cell.Y >= BoardSize)
            {
                return false;
            }

            if (occupied.Contains(cell))
            {
                return false;
            }
        }

        return true;
    }

    private static void MarkOccupiedCells(Ship ship, HashSet<Coordinate> occupied)
    {
        for (int i = 0; i < ship.Length; i++)
        {
            var cell = ship.Orientation == Orientation.Horizontal
                ? new Coordinate(ship.StartPosition.X + i, ship.StartPosition.Y)
                : new Coordinate(ship.StartPosition.X, ship.StartPosition.Y + i);

            occupied.Add(cell);
        }
    }
}
