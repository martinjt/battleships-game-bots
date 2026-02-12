using BattleshipsBot.Common.Interfaces;

namespace DiagonalDominion;

/// <summary>
/// Fires along diagonals from bottom-left to top-right with parity optimization.
/// When a hit is detected, switches to target mode for directional probing.
/// </summary>
public class DiagonalSweepFiringStrategy : IFiringStrategy
{
    private const int BoardSize = 10;

    private readonly HashSet<Coordinate> _firedShots = new();
    private readonly HashSet<Coordinate> _hits = new();
    private readonly Queue<Coordinate> _targetQueue = new();
    private readonly List<Coordinate> _diagonalScanOrder;
    private int _scanIndex;
    private Coordinate? _lastHit;

    public DiagonalSweepFiringStrategy()
    {
        _diagonalScanOrder = GenerateDiagonalScanOrder();
        _scanIndex = 0;
    }

    /// <summary>
    /// Generates scan order along diagonals (bottom-left to top-right) with parity optimization.
    /// Diagonals are scanned in order: main diagonal, then alternating above/below.
    /// Only cells where (x+y)%2 == 0 for optimal coverage of ships of length >= 2.
    /// </summary>
    private static List<Coordinate> GenerateDiagonalScanOrder()
    {
        var result = new List<Coordinate>();

        // Scan diagonals from the main diagonal outward
        // Main diagonal goes from (0,9) to (9,0) - sum = 9
        // Then alternate between sums 7,11,5,13,3,15,1,17 (staying within bounds)

        var diagonalSums = new List<int>();

        // Start with even sums for parity optimization (x+y) % 2 == 0
        // We want cells where the diagonal sum matches parity
        for (int sum = 8; sum >= 0; sum -= 2) diagonalSums.Add(sum);
        for (int sum = 10; sum <= 18; sum += 2) diagonalSums.Add(sum);

        // Then fill in odd sums
        for (int sum = 9; sum >= 1; sum -= 2) diagonalSums.Add(sum);
        for (int sum = 11; sum <= 17; sum += 2) diagonalSums.Add(sum);

        foreach (var sum in diagonalSums)
        {
            // For diagonal with x + y = sum, x ranges from max(0, sum-9) to min(9, sum)
            int xStart = Math.Max(0, sum - (BoardSize - 1));
            int xEnd = Math.Min(BoardSize - 1, sum);

            for (int x = xStart; x <= xEnd; x++)
            {
                int y = sum - x;
                if (y >= 0 && y < BoardSize)
                {
                    result.Add(new Coordinate(x, y));
                }
            }
        }

        return result;
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

        // Priority 2: Continue diagonal scan
        while (_scanIndex < _diagonalScanOrder.Count)
        {
            var shot = _diagonalScanOrder[_scanIndex++];
            if (!_firedShots.Contains(shot))
            {
                _firedShots.Add(shot);
                return shot;
            }
        }

        // Fallback: Find any unfired cell (shouldn't normally reach here)
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                var coord = new Coordinate(x, y);
                if (!_firedShots.Contains(coord))
                {
                    _firedShots.Add(coord);
                    return coord;
                }
            }
        }

        // Board exhausted - return origin (game should be over)
        return new Coordinate(0, 0);
    }

    public void RecordHit(Coordinate coordinate)
    {
        _firedShots.Add(coordinate);

        if (!_hits.Contains(coordinate))
        {
            _hits.Add(coordinate);
            _lastHit = coordinate;

            // Add adjacent cells to target queue for directional probing
            EnqueueAdjacentTargets(coordinate);
        }
    }

    public void RecordMiss(Coordinate coordinate)
    {
        _firedShots.Add(coordinate);
    }

    public void RecordSunk()
    {
        // Clear target queue when a ship is sunk
        _targetQueue.Clear();
        _lastHit = null;
    }

    public void Reset()
    {
        _firedShots.Clear();
        _hits.Clear();
        _targetQueue.Clear();
        _scanIndex = 0;
        _lastHit = null;
    }

    private void EnqueueAdjacentTargets(Coordinate center)
    {
        // Add orthogonal neighbors in priority order (based on hit patterns)
        var directions = new[]
        {
            new Coordinate(0, -1),  // Up
            new Coordinate(0, 1),   // Down
            new Coordinate(-1, 0),  // Left
            new Coordinate(1, 0),   // Right
        };

        foreach (var dir in directions)
        {
            var target = new Coordinate(center.X + dir.X, center.Y + dir.Y);
            if (IsValidCoordinate(target) && !_firedShots.Contains(target))
            {
                _targetQueue.Enqueue(target);
            }
        }
    }

    private static bool IsValidCoordinate(Coordinate coord)
    {
        return coord.X >= 0 && coord.X < BoardSize && coord.Y >= 0 && coord.Y < BoardSize;
    }
}
