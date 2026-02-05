namespace EdgeHunter;

/// <summary>
/// Places ships in the center of the board to counter edge-targeting opponents.
/// Ships cluster around the center region (3,3) to (6,6) with some randomization.
/// </summary>
public class CenterClusterShipPlacer : IShipPlacer
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
            var ship = PlaceShipInCenter(name, length, occupiedCells);
            ships.Add(ship);
            MarkOccupiedCells(ship, occupiedCells);
        }

        return ships;
    }

    private static Ship PlaceShipInCenter(string name, int length, HashSet<Coordinate> occupied)
    {
        // Generate candidates with center bias
        var candidates = GenerateCenterBiasCandidates(name, length, occupied);

        if (candidates.Count > 0)
        {
            return candidates[Random.Next(candidates.Count)];
        }

        // Fallback to any valid position
        return FindAnyValidPlacement(name, length, occupied);
    }

    private static List<Ship> GenerateCenterBiasCandidates(string name, int length, HashSet<Coordinate> occupied)
    {
        var candidates = new List<Ship>();

        // Center region: prioritize positions where ship overlaps center area (2,2) to (7,7)
        int centerMin = 2;
        int centerMax = 7;

        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                var start = new Coordinate(x, y);

                foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
                {
                    if (CanPlaceShip(start, length, orientation, occupied))
                    {
                        var ship = new Ship(name, length, start, orientation);

                        // Calculate how many cells are in the center region
                        int centerCells = CountCellsInRegion(start, length, orientation, centerMin, centerMax);

                        // Weight based on center coverage
                        if (centerCells == length)
                        {
                            // Fully in center - highest weight
                            for (int i = 0; i < 4; i++) candidates.Add(ship);
                        }
                        else if (centerCells > 0)
                        {
                            // Partially in center - medium weight
                            for (int i = 0; i < 2; i++) candidates.Add(ship);
                        }
                        // No cells in center - don't add to candidates unless needed
                    }
                }
            }
        }

        // If no center candidates, allow edge positions
        if (candidates.Count == 0)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    var start = new Coordinate(x, y);
                    foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
                    {
                        if (CanPlaceShip(start, length, orientation, occupied))
                        {
                            candidates.Add(new Ship(name, length, start, orientation));
                        }
                    }
                }
            }
        }

        return candidates;
    }

    private static int CountCellsInRegion(Coordinate start, int length, Orientation orientation, int minBound, int maxBound)
    {
        int count = 0;
        for (int i = 0; i < length; i++)
        {
            var cell = orientation == Orientation.Horizontal
                ? new Coordinate(start.X + i, start.Y)
                : new Coordinate(start.X, start.Y + i);

            if (cell.X >= minBound && cell.X <= maxBound && cell.Y >= minBound && cell.Y <= maxBound)
            {
                count++;
            }
        }
        return count;
    }

    private static Ship FindAnyValidPlacement(string name, int length, HashSet<Coordinate> occupied)
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
