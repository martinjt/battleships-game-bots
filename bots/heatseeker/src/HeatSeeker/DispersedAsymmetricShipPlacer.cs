using BattleshipsBot.Common.Interfaces;

namespace HeatSeeker;

/// <summary>
/// Maximum dispersion asymmetric ship placement pattern.
/// Ships are spread across the board to maximize the number of shots needed to find them.
/// Avoids predictable patterns by using asymmetric placement.
/// </summary>
public class DispersedAsymmetricShipPlacer : IShipPlacer
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
            var ship = PlaceShipDispersed(name, length, occupiedCells, ships);
            ships.Add(ship);
            MarkOccupiedCells(ship, occupiedCells);
        }

        return ships;
    }

    private static Ship PlaceShipDispersed(string name, int length, HashSet<Coordinate> occupied, List<Ship> placedShips)
    {
        var candidates = GenerateDispersedCandidates(name, length, occupied, placedShips);

        if (candidates.Count > 0)
        {
            return candidates[Random.Next(candidates.Count)];
        }

        return FindAnyValidPlacement(name, length, occupied);
    }

    private static List<Ship> GenerateDispersedCandidates(string name, int length, HashSet<Coordinate> occupied, List<Ship> placedShips)
    {
        var candidates = new List<Ship>();
        var bestDistance = 0.0;

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
                        var distance = CalculateMinDistanceToOtherShips(ship, placedShips);

                        // Asymmetry bonus: avoid placing at symmetric positions
                        var asymmetryBonus = CalculateAsymmetryBonus(ship);
                        var totalScore = distance + asymmetryBonus;

                        if (totalScore > bestDistance + 0.5)
                        {
                            bestDistance = totalScore;
                            candidates.Clear();
                            candidates.Add(ship);
                        }
                        else if (Math.Abs(totalScore - bestDistance) < 0.5)
                        {
                            candidates.Add(ship);
                        }
                    }
                }
            }
        }

        return candidates;
    }

    private static double CalculateMinDistanceToOtherShips(Ship newShip, List<Ship> placedShips)
    {
        if (placedShips.Count == 0)
        {
            // For first ship, prefer positions not in center or corners
            var centerX = newShip.StartPosition.X + (newShip.Orientation == Orientation.Horizontal ? newShip.Length / 2.0 : 0);
            var centerY = newShip.StartPosition.Y + (newShip.Orientation == Orientation.Vertical ? newShip.Length / 2.0 : 0);

            // Distance from board center (4.5, 4.5)
            var distFromCenter = Math.Sqrt(Math.Pow(centerX - 4.5, 2) + Math.Pow(centerY - 4.5, 2));

            // Prefer some distance from center but not at the very edge
            return distFromCenter > 3 && distFromCenter < 5 ? 5 : distFromCenter * 0.5;
        }

        double minDistance = double.MaxValue;

        var newShipCells = GetShipCells(newShip);

        foreach (var placedShip in placedShips)
        {
            var placedCells = GetShipCells(placedShip);

            foreach (var newCell in newShipCells)
            {
                foreach (var placedCell in placedCells)
                {
                    var distance = Math.Abs(newCell.X - placedCell.X) + Math.Abs(newCell.Y - placedCell.Y);
                    minDistance = Math.Min(minDistance, distance);
                }
            }
        }

        return minDistance;
    }

    private static double CalculateAsymmetryBonus(Ship ship)
    {
        // Penalize positions that are symmetric (e.g., (2,3) and (7,6) are symmetric about center)
        var centerX = ship.StartPosition.X + (ship.Orientation == Orientation.Horizontal ? ship.Length / 2.0 : 0);
        var centerY = ship.StartPosition.Y + (ship.Orientation == Orientation.Vertical ? ship.Length / 2.0 : 0);

        // Distance from diagonal symmetry (where x == y)
        var diagDistance = Math.Abs(centerX - centerY);

        // Distance from anti-diagonal (where x + y == 9)
        var antiDiagDistance = Math.Abs((centerX + centerY) - 9);

        // Bonus for positions that are off both diagonals
        return (diagDistance * 0.3) + (antiDiagDistance * 0.3);
    }

    private static List<Coordinate> GetShipCells(Ship ship)
    {
        var cells = new List<Coordinate>();
        for (int i = 0; i < ship.Length; i++)
        {
            var cell = ship.Orientation == Orientation.Horizontal
                ? new Coordinate(ship.StartPosition.X + i, ship.StartPosition.Y)
                : new Coordinate(ship.StartPosition.X, ship.StartPosition.Y + i);
            cells.Add(cell);
        }
        return cells;
    }

    private static Ship FindAnyValidPlacement(string name, int length, HashSet<Coordinate> occupied)
    {
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
