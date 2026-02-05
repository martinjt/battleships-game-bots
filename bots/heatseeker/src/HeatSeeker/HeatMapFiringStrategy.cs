namespace HeatSeeker;

/// <summary>
/// Monte Carlo-style heat map probability targeting.
/// Calculates probability density for each cell based on remaining ship configurations.
/// Dynamically updates heat map on hits/misses/sinks.
/// </summary>
public class HeatMapFiringStrategy : IFiringStrategy
{
    private const int BoardSize = 10;

    private readonly HashSet<Coordinate> _firedShots = new();
    private readonly HashSet<Coordinate> _hits = new();
    private readonly Queue<Coordinate> _targetQueue = new();
    private readonly List<int> _remainingShipLengths;

    private static readonly int[] InitialShipLengths = { 5, 4, 3, 3, 2 };

    public HeatMapFiringStrategy()
    {
        _remainingShipLengths = new List<int>(InitialShipLengths);
    }

    public Coordinate GetNextShot()
    {
        // Priority 1: Process target queue (hunt mode after a hit)
        while (_targetQueue.Count > 0)
        {
            var target = _targetQueue.Dequeue();
            if (!_firedShots.Contains(target) && IsValidCoordinate(target))
            {
                _firedShots.Add(target);
                return target;
            }
        }

        // Priority 2: Calculate heat map and fire at highest probability cell
        var heatMap = CalculateHeatMap();
        var bestTarget = FindBestTarget(heatMap);

        _firedShots.Add(bestTarget);
        return bestTarget;
    }

    private double[,] CalculateHeatMap()
    {
        var heatMap = new double[BoardSize, BoardSize];

        // For each remaining ship, calculate where it could be placed
        foreach (var shipLength in _remainingShipLengths)
        {
            // Try all horizontal placements
            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x <= BoardSize - shipLength; x++)
                {
                    if (CanPlaceShip(x, y, shipLength, Orientation.Horizontal))
                    {
                        // Add probability to each cell the ship would occupy
                        double weight = CalculatePlacementWeight(x, y, shipLength, Orientation.Horizontal);
                        for (int i = 0; i < shipLength; i++)
                        {
                            heatMap[x + i, y] += weight;
                        }
                    }
                }
            }

            // Try all vertical placements
            for (int y = 0; y <= BoardSize - shipLength; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    if (CanPlaceShip(x, y, shipLength, Orientation.Vertical))
                    {
                        double weight = CalculatePlacementWeight(x, y, shipLength, Orientation.Vertical);
                        for (int i = 0; i < shipLength; i++)
                        {
                            heatMap[x, y + i] += weight;
                        }
                    }
                }
            }
        }

        // Apply parity bonus: cells where (x+y) % 2 == 0 have higher probability
        // (optimal for hitting ships of length >= 2)
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if ((x + y) % 2 == 0 && heatMap[x, y] > 0)
                {
                    heatMap[x, y] *= 1.2;
                }
            }
        }

        // Boost cells adjacent to existing hits (strongly)
        foreach (var hit in _hits)
        {
            var adjacents = GetAdjacentCells(hit);
            foreach (var adj in adjacents)
            {
                if (!_firedShots.Contains(adj) && IsValidCoordinate(adj))
                {
                    heatMap[adj.X, adj.Y] *= 3.0;
                }
            }
        }

        // Zero out already fired cells
        foreach (var shot in _firedShots)
        {
            heatMap[shot.X, shot.Y] = 0;
        }

        return heatMap;
    }

    private bool CanPlaceShip(int startX, int startY, int length, Orientation orientation)
    {
        for (int i = 0; i < length; i++)
        {
            int x = orientation == Orientation.Horizontal ? startX + i : startX;
            int y = orientation == Orientation.Vertical ? startY + i : startY;

            var coord = new Coordinate(x, y);

            // Can't place if we've hit a miss there
            if (_firedShots.Contains(coord) && !_hits.Contains(coord))
            {
                return false;
            }
        }
        return true;
    }

    private double CalculatePlacementWeight(int startX, int startY, int length, Orientation orientation)
    {
        double weight = 1.0;
        int hitCount = 0;

        for (int i = 0; i < length; i++)
        {
            int x = orientation == Orientation.Horizontal ? startX + i : startX;
            int y = orientation == Orientation.Vertical ? startY + i : startY;

            var coord = new Coordinate(x, y);

            // Higher weight if this placement includes an existing hit
            if (_hits.Contains(coord))
            {
                hitCount++;
            }
        }

        // Placements that include existing hits are much more likely to be correct
        if (hitCount > 0)
        {
            weight *= Math.Pow(5.0, hitCount);
        }

        return weight;
    }

    private Coordinate FindBestTarget(double[,] heatMap)
    {
        double maxHeat = -1;
        var candidates = new List<Coordinate>();

        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                // Skip already fired cells
                if (_firedShots.Contains(new Coordinate(x, y)))
                {
                    continue;
                }

                if (heatMap[x, y] > maxHeat)
                {
                    maxHeat = heatMap[x, y];
                    candidates.Clear();
                    candidates.Add(new Coordinate(x, y));
                }
                else if (Math.Abs(heatMap[x, y] - maxHeat) < 0.001)
                {
                    candidates.Add(new Coordinate(x, y));
                }
            }
        }

        if (candidates.Count == 0)
        {
            // Fallback: find any unfired cell
            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    var coord = new Coordinate(x, y);
                    if (!_firedShots.Contains(coord))
                    {
                        return coord;
                    }
                }
            }
            return new Coordinate(0, 0);
        }

        // If multiple cells have same heat, pick randomly among them
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    public void RecordHit(Coordinate coordinate)
    {
        _firedShots.Add(coordinate);

        if (!_hits.Contains(coordinate))
        {
            _hits.Add(coordinate);

            // Queue adjacent cells for investigation
            EnqueueAdjacentTargets(coordinate);
        }
    }

    public void RecordMiss(Coordinate coordinate)
    {
        _firedShots.Add(coordinate);
    }

    public void RecordSunk()
    {
        // Remove a ship from remaining (we don't know which, so remove smallest for conservative estimate)
        if (_remainingShipLengths.Count > 0)
        {
            int minIndex = 0;
            int minLength = _remainingShipLengths[0];
            for (int i = 1; i < _remainingShipLengths.Count; i++)
            {
                if (_remainingShipLengths[i] < minLength)
                {
                    minLength = _remainingShipLengths[i];
                    minIndex = i;
                }
            }
            _remainingShipLengths.RemoveAt(minIndex);
        }

        // Clear target queue
        _targetQueue.Clear();
    }

    public void Reset()
    {
        _firedShots.Clear();
        _hits.Clear();
        _targetQueue.Clear();
        _remainingShipLengths.Clear();
        _remainingShipLengths.AddRange(InitialShipLengths);
    }

    private void EnqueueAdjacentTargets(Coordinate center)
    {
        foreach (var adj in GetAdjacentCells(center))
        {
            if (!_firedShots.Contains(adj) && IsValidCoordinate(adj))
            {
                _targetQueue.Enqueue(adj);
            }
        }
    }

    private static List<Coordinate> GetAdjacentCells(Coordinate center)
    {
        return new List<Coordinate>
        {
            new Coordinate(center.X, center.Y - 1),
            new Coordinate(center.X, center.Y + 1),
            new Coordinate(center.X - 1, center.Y),
            new Coordinate(center.X + 1, center.Y)
        };
    }

    private static bool IsValidCoordinate(Coordinate coord)
    {
        return coord.X >= 0 && coord.X < BoardSize && coord.Y >= 0 && coord.Y < BoardSize;
    }
}
