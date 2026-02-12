using BattleshipsBot.Common.Interfaces;

namespace EdgeHunter;

/// <summary>
/// Ring-based scan starting from outer edges, working inward.
/// Exploits common tendency to place ships along board edges.
/// Uses 2-cell spacing with ship-size awareness.
/// </summary>
public class PerimeterInFiringStrategy : IFiringStrategy
{
    private const int BoardSize = 10;

    private readonly HashSet<Coordinate> _firedShots = new();
    private readonly HashSet<Coordinate> _hits = new();
    private readonly Queue<Coordinate> _targetQueue = new();
    private readonly List<Coordinate> _perimeterScanOrder;
    private int _scanIndex;

    public PerimeterInFiringStrategy()
    {
        _perimeterScanOrder = GeneratePerimeterScanOrder();
        _scanIndex = 0;
    }

    /// <summary>
    /// Generates scan order in concentric rings from edges to center.
    /// Ring 0: edges (row 0, row 9, col 0, col 9) with 2-cell gaps
    /// Ring 1: (row 1, row 8, col 1, col 8)
    /// ...continue inward
    /// </summary>
    private static List<Coordinate> GeneratePerimeterScanOrder()
    {
        var result = new List<Coordinate>();
        var added = new HashSet<Coordinate>();

        // Process rings from outside to inside
        for (int ring = 0; ring <= 4; ring++)
        {
            var ringCells = GetRingCells(ring);

            // Add cells with 2-cell spacing pattern for optimal coverage
            // First pass: cells where (x+y) % 2 == ring % 2 for checkerboard-like coverage
            foreach (var cell in ringCells)
            {
                if ((cell.X + cell.Y) % 2 == ring % 2 && !added.Contains(cell))
                {
                    result.Add(cell);
                    added.Add(cell);
                }
            }

            // Second pass: remaining cells in this ring
            foreach (var cell in ringCells)
            {
                if (!added.Contains(cell))
                {
                    result.Add(cell);
                    added.Add(cell);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all cells in a given ring (0 = outermost, 4 = center)
    /// </summary>
    private static List<Coordinate> GetRingCells(int ring)
    {
        var cells = new List<Coordinate>();
        int min = ring;
        int max = BoardSize - 1 - ring;

        if (min > max) return cells;

        // Top row of ring (left to right)
        for (int x = min; x <= max; x++)
        {
            cells.Add(new Coordinate(x, min));
        }

        // Right column of ring (top to bottom, excluding corners)
        for (int y = min + 1; y <= max - 1; y++)
        {
            cells.Add(new Coordinate(max, y));
        }

        // Bottom row of ring (right to left)
        if (min != max)
        {
            for (int x = max; x >= min; x--)
            {
                cells.Add(new Coordinate(x, max));
            }
        }

        // Left column of ring (bottom to top, excluding corners)
        for (int y = max - 1; y >= min + 1; y--)
        {
            cells.Add(new Coordinate(min, y));
        }

        return cells;
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

        // Priority 2: Continue perimeter scan
        while (_scanIndex < _perimeterScanOrder.Count)
        {
            var shot = _perimeterScanOrder[_scanIndex++];
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

            // Add adjacent cells to target queue for hunt mode
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
    }

    public void Reset()
    {
        _firedShots.Clear();
        _hits.Clear();
        _targetQueue.Clear();
        _scanIndex = 0;
    }

    private void EnqueueAdjacentTargets(Coordinate center)
    {
        // Add orthogonal neighbors - prioritize along edges if near edges
        var directions = new[]
        {
            new Coordinate(0, -1),  // Up
            new Coordinate(0, 1),   // Down
            new Coordinate(-1, 0),  // Left
            new Coordinate(1, 0),   // Right
        };

        // If hit is on an edge, prioritize along the edge
        bool onTopEdge = center.Y == 0;
        bool onBottomEdge = center.Y == BoardSize - 1;
        bool onLeftEdge = center.X == 0;
        bool onRightEdge = center.X == BoardSize - 1;

        var prioritizedDirections = new List<Coordinate>();

        if (onTopEdge || onBottomEdge)
        {
            // Prioritize horizontal movement along edges
            prioritizedDirections.Add(new Coordinate(-1, 0));
            prioritizedDirections.Add(new Coordinate(1, 0));
            prioritizedDirections.Add(new Coordinate(0, -1));
            prioritizedDirections.Add(new Coordinate(0, 1));
        }
        else if (onLeftEdge || onRightEdge)
        {
            // Prioritize vertical movement along edges
            prioritizedDirections.Add(new Coordinate(0, -1));
            prioritizedDirections.Add(new Coordinate(0, 1));
            prioritizedDirections.Add(new Coordinate(-1, 0));
            prioritizedDirections.Add(new Coordinate(1, 0));
        }
        else
        {
            prioritizedDirections.AddRange(directions);
        }

        foreach (var dir in prioritizedDirections)
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
