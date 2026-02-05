namespace SharpShooter;

/// <summary>
/// Adaptive firing strategy that counters opponent placement patterns
/// </summary>
public class AdaptiveFiringStrategy : IFiringStrategy
{
    private const int BoardSize = 10;
    private readonly OpponentDetector _opponentDetector;
    private readonly HashSet<Coordinate> _shotsFired = new();
    private readonly Queue<Coordinate> _targetQueue = new();

    // Hunt mode state
    private bool _inHuntMode = false;
    private Coordinate? _lastHit = null;
    private readonly List<Coordinate> _currentShipHits = new();

    public AdaptiveFiringStrategy(OpponentDetector opponentDetector)
    {
        _opponentDetector = opponentDetector;
    }

    public Coordinate GetNextShot()
    {
        // If we have queued targets from hunt mode, use those first
        while (_targetQueue.Count > 0)
        {
            var target = _targetQueue.Dequeue();
            if (!_shotsFired.Contains(target) && IsValidCoordinate(target))
            {
                _shotsFired.Add(target);
                return target;
            }
        }

        // Otherwise, use strategy based on opponent
        var strategy = _opponentDetector.GetDetectedStrategy();
        var shot = strategy switch
        {
            OpponentStrategy.CenterFirst => GetShotAntiCenterFirst(),
            OpponentStrategy.CornerCheckerboard => GetShotAntiCheckerboard(),
            _ => GetShotCheckerboard() // Default to checkerboard (optimal)
        };

        _shotsFired.Add(shot);
        return shot;
    }

    /// <summary>
    /// Record a hit to enable hunt mode (idempotent - safe to call multiple times)
    /// </summary>
    public void RecordHit(Coordinate hit)
    {
        // Make idempotent - don't re-process if already recorded
        if (_shotsFired.Contains(hit))
        {
            return;
        }

        _shotsFired.Add(hit);
        _inHuntMode = true;
        _lastHit = hit;
        _currentShipHits.Add(hit);

        // Queue adjacent cells for testing
        QueueAdjacentCells(hit);
    }

    /// <summary>
    /// Record a miss (idempotent - safe to call multiple times)
    /// </summary>
    public void RecordMiss(Coordinate miss)
    {
        _shotsFired.Add(miss);
    }

    /// <summary>
    /// Record a ship being sunk to exit hunt mode
    /// </summary>
    public void RecordSunk()
    {
        _inHuntMode = false;
        _lastHit = null;
        _currentShipHits.Clear();
        _targetQueue.Clear();
    }

    public void Reset()
    {
        _shotsFired.Clear();
        _targetQueue.Clear();
        _inHuntMode = false;
        _lastHit = null;
        _currentShipHits.Clear();
    }

    /// <summary>
    /// Counter DepthCharge's edge placement
    /// Use checkerboard from corner (like Mirage)
    /// </summary>
    private Coordinate GetShotAntiCenterFirst()
    {
        // Use checkerboard starting from corner
        // This efficiently finds ships on edges
        return GetShotCheckerboard();
    }

    /// <summary>
    /// Counter Mirage's odd-column placement
    /// Use center-first spiral to hit even columns and rows
    /// </summary>
    private Coordinate GetShotAntiCheckerboard()
    {
        // Start from center and spiral outward
        // Prioritize even columns/rows (opposite of Mirage's checkerboard)
        for (int radius = 0; radius < BoardSize; radius++)
        {
            var candidates = new List<Coordinate>();

            // Check all positions at this radius from center
            for (int x = Math.Max(0, 4 - radius); x <= Math.Min(9, 4 + radius); x++)
            {
                for (int y = Math.Max(0, 4 - radius); y <= Math.Min(9, 4 + radius); y++)
                {
                    // Only check positions at the edge of this radius
                    if (Math.Abs(x - 4) == radius || Math.Abs(y - 4) == radius)
                    {
                        var coord = new Coordinate(x, y);
                        if (!_shotsFired.Contains(coord))
                        {
                            candidates.Add(coord);
                        }
                    }
                }
            }

            if (candidates.Count > 0)
            {
                // Prefer even columns or even rows
                var evenCandidates = candidates.Where(c => c.X % 2 == 0 || c.Y % 2 == 0).ToList();
                if (evenCandidates.Count > 0)
                {
                    return evenCandidates.First();
                }
                return candidates.First();
            }
        }

        // Fallback: any unshot position
        return GetFirstUnshotPosition();
    }

    /// <summary>
    /// Optimal checkerboard search pattern
    /// Starts from bottom-right corner like Mirage
    /// </summary>
    private Coordinate GetShotCheckerboard()
    {
        // Search bottom to top, alternating parity
        for (int row = BoardSize - 1; row >= 0; row--)
        {
            // Odd rows: odd columns (9,7,5,3,1)
            // Even rows: even columns (8,6,4,2,0)
            bool oddRow = row % 2 == 1;

            for (int col = BoardSize - 1; col >= 0; col--)
            {
                bool oddCol = col % 2 == 1;

                // Checkerboard pattern: odd row needs odd col, even row needs even col
                if (oddRow == oddCol)
                {
                    var coord = new Coordinate(col, row);
                    if (!_shotsFired.Contains(coord))
                    {
                        return coord;
                    }
                }
            }
        }

        // If checkerboard complete, fill in the gaps
        return GetFirstUnshotPosition();
    }

    private void QueueAdjacentCells(Coordinate center)
    {
        // Add all 4 adjacent cells
        var adjacent = new[]
        {
            new Coordinate(center.X, center.Y - 1), // North
            new Coordinate(center.X, center.Y + 1), // South
            new Coordinate(center.X - 1, center.Y), // West
            new Coordinate(center.X + 1, center.Y)  // East
        };

        foreach (var cell in adjacent)
        {
            if (IsValidCoordinate(cell) && !_shotsFired.Contains(cell))
            {
                _targetQueue.Enqueue(cell);
            }
        }
    }

    private Coordinate GetFirstUnshotPosition()
    {
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                var coord = new Coordinate(x, y);
                if (!_shotsFired.Contains(coord))
                {
                    return coord;
                }
            }
        }

        // Should never reach here in normal game
        return new Coordinate(0, 0);
    }

    private static bool IsValidCoordinate(Coordinate coord)
    {
        return coord.X >= 0 && coord.X < BoardSize &&
               coord.Y >= 0 && coord.Y < BoardSize;
    }
}
