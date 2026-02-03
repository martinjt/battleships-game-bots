namespace StackOverflowAttack.Strategies;

public class ProbabilityDensityFiringStrategy : IFiringStrategy
{
    private const int BoardSize = 10;
    private readonly int[,] _probabilityGrid = new int[BoardSize, BoardSize];
    private readonly HashSet<Coordinate> _firedShots = new();
    private readonly HashSet<Coordinate> _hits = new();
    private readonly Queue<Coordinate> _targetQueue = new();
    private readonly List<Ship> _remainingShips;
    private bool _inTargetMode = false;
    private Coordinate? _lastHit = null;

    public ProbabilityDensityFiringStrategy()
    {
        // Initialize with all standard ship sizes
        _remainingShips = new List<Ship>
        {
            new Ship("Carrier", 5),
            new Ship("Battleship", 4),
            new Ship("Cruiser", 3),
            new Ship("Submarine", 3),
            new Ship("Destroyer", 2)
        };
    }

    public Coordinate GetNextShot()
    {
        // Target mode: if we have targets in queue, fire at them
        if (_inTargetMode && _targetQueue.Count > 0)
        {
            var target = _targetQueue.Dequeue();

            // Skip if already fired at this position
            while (_firedShots.Contains(target) && _targetQueue.Count > 0)
            {
                target = _targetQueue.Dequeue();
            }

            if (!_firedShots.Contains(target))
            {
                _firedShots.Add(target);
                return target;
            }

            // If all targets exhausted, switch back to hunt mode
            _inTargetMode = false;
        }

        // Hunt mode: calculate probability density and fire at highest probability cell
        CalculateProbabilityDensity();
        var nextShot = GetHighestProbabilityCell();
        _firedShots.Add(nextShot);
        return nextShot;
    }

    public void RecordHit(Coordinate coordinate)
    {
        // Add to fired shots to ensure we don't fire here again
        _firedShots.Add(coordinate);

        // Only process new hits to avoid duplicate target queue entries
        if (!_hits.Contains(coordinate))
        {
            _hits.Add(coordinate);
            _lastHit = coordinate;

            // Switch to target mode and add adjacent cells to target queue
            _inTargetMode = true;
            AddAdjacentCellsToTargetQueue(coordinate);
        }
    }

    public void RecordMiss(Coordinate coordinate)
    {
        // Add to fired shots to ensure we don't fire here again
        _firedShots.Add(coordinate);
    }

    public void RecordSunk(string shipName)
    {
        // Remove the sunk ship from remaining ships
        var ship = _remainingShips.FirstOrDefault(s => s.Name.Equals(shipName, StringComparison.OrdinalIgnoreCase));
        if (ship != null)
        {
            _remainingShips.Remove(ship);
        }

        // Clear target queue and exit target mode when ship is sunk
        _targetQueue.Clear();
        _inTargetMode = false;
        _lastHit = null;
    }

    public void Reset()
    {
        _firedShots.Clear();
        _hits.Clear();
        _targetQueue.Clear();
        _inTargetMode = false;
        _lastHit = null;

        // Reset ship list
        _remainingShips.Clear();
        _remainingShips.Add(new Ship("Carrier", 5));
        _remainingShips.Add(new Ship("Battleship", 4));
        _remainingShips.Add(new Ship("Cruiser", 3));
        _remainingShips.Add(new Ship("Submarine", 3));
        _remainingShips.Add(new Ship("Destroyer", 2));
    }

    private void CalculateProbabilityDensity()
    {
        // Reset probability grid
        Array.Clear(_probabilityGrid, 0, _probabilityGrid.Length);

        foreach (var ship in _remainingShips)
        {
            // Try all possible horizontal placements
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col <= BoardSize - ship.Length; col++)
                {
                    if (CanPlaceShip(row, col, ship.Length, Orientation.Horizontal))
                    {
                        // Increment probability for each cell this ship could occupy
                        for (int i = 0; i < ship.Length; i++)
                        {
                            _probabilityGrid[row, col + i]++;
                        }
                    }
                }
            }

            // Try all possible vertical placements
            for (int row = 0; row <= BoardSize - ship.Length; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if (CanPlaceShip(row, col, ship.Length, Orientation.Vertical))
                    {
                        // Increment probability for each cell this ship could occupy
                        for (int i = 0; i < ship.Length; i++)
                        {
                            _probabilityGrid[row + i, col]++;
                        }
                    }
                }
            }
        }
    }

    private bool CanPlaceShip(int startRow, int startCol, int length, Orientation orientation)
    {
        for (int i = 0; i < length; i++)
        {
            int row = orientation == Orientation.Horizontal ? startRow : startRow + i;
            int col = orientation == Orientation.Horizontal ? startCol + i : startCol;

            var coord = new Coordinate(col, row);

            // Can't place if we've already fired here or it's a known miss
            if (_firedShots.Contains(coord))
            {
                // If it's a hit, we need the entire ship to overlap hits
                // Otherwise, this placement is invalid
                if (!_hits.Contains(coord))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private Coordinate GetHighestProbabilityCell()
    {
        int maxProbability = -1;
        var candidates = new List<Coordinate>();

        // Apply parity pattern: prefer checkerboard squares (x + y) % 2 == 0
        // This halves the search space since smallest ship is 2 squares
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                var coord = new Coordinate(col, row);

                // Skip already fired shots
                if (_firedShots.Contains(coord))
                {
                    continue;
                }

                int probability = _probabilityGrid[row, col];

                // Boost probability for checkerboard pattern
                if ((row + col) % 2 == 0)
                {
                    probability += 1;
                }

                if (probability > maxProbability)
                {
                    maxProbability = probability;
                    candidates.Clear();
                    candidates.Add(coord);
                }
                else if (probability == maxProbability)
                {
                    candidates.Add(coord);
                }
            }
        }

        // If we have multiple candidates with same probability, pick center-most
        // Ships are more likely to be near the center
        if (candidates.Count > 0)
        {
            return candidates
                .OrderBy(c => Math.Abs(c.X - 4.5) + Math.Abs(c.Y - 4.5))
                .First();
        }

        // Fallback: find any unfired cell (shouldn't happen)
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                var coord = new Coordinate(col, row);
                if (!_firedShots.Contains(coord))
                {
                    return coord;
                }
            }
        }

        // Should never reach here
        return new Coordinate(0, 0);
    }

    private void AddAdjacentCellsToTargetQueue(Coordinate hit)
    {
        // Add all four adjacent cells (up, down, left, right)
        var adjacentCells = new[]
        {
            new Coordinate(hit.X, hit.Y - 1),  // Up
            new Coordinate(hit.X, hit.Y + 1),  // Down
            new Coordinate(hit.X - 1, hit.Y),  // Left
            new Coordinate(hit.X + 1, hit.Y)   // Right
        };

        foreach (var cell in adjacentCells)
        {
            // Check if cell is within bounds and not already fired
            if (cell.X >= 0 && cell.X < BoardSize &&
                cell.Y >= 0 && cell.Y < BoardSize &&
                !_firedShots.Contains(cell) &&
                !_targetQueue.Contains(cell))
            {
                _targetQueue.Enqueue(cell);
            }
        }
    }

    private class Ship
    {
        public string Name { get; }
        public int Length { get; }

        public Ship(string name, int length)
        {
            Name = name;
            Length = length;
        }
    }
}
