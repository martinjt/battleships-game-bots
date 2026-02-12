using BattleshipsBot.Common.Interfaces;

namespace SharpShooter;

/// <summary>
/// Detects opponent strategy based on their shot patterns
/// </summary>
public enum OpponentStrategy
{
    Unknown,
    CenterFirst,    // Like DepthCharge (starts at 4,4)
    CornerCheckerboard,  // Like Mirage (starts at corner with checkerboard)
    Other
}

public class OpponentDetector
{
    private readonly List<Coordinate> _opponentShots = new();
    private OpponentStrategy _detectedStrategy = OpponentStrategy.Unknown;
    private bool _detectionComplete = false;

    /// <summary>
    /// Record an opponent's shot for pattern analysis
    /// </summary>
    public void RecordOpponentShot(Coordinate shot)
    {
        _opponentShots.Add(shot);

        // Try to detect after 3-5 shots
        if (!_detectionComplete && _opponentShots.Count >= 3)
        {
            DetectStrategy();
        }
    }

    /// <summary>
    /// Get the detected opponent strategy
    /// </summary>
    public OpponentStrategy GetDetectedStrategy()
    {
        return _detectedStrategy;
    }

    /// <summary>
    /// Reset detection for a new game
    /// </summary>
    public void Reset()
    {
        _opponentShots.Clear();
        _detectedStrategy = OpponentStrategy.Unknown;
        _detectionComplete = false;
    }

    private void DetectStrategy()
    {
        if (_opponentShots.Count < 3)
        {
            return;
        }

        var firstShot = _opponentShots[0];

        // Check for center-first strategy (DepthCharge pattern)
        // DepthCharge starts at (4,4) or near center
        if (IsCenterPosition(firstShot))
        {
            _detectedStrategy = OpponentStrategy.CenterFirst;
            _detectionComplete = true;
            return;
        }

        // Check for corner checkerboard (Mirage pattern)
        // Mirage starts at a corner and follows checkerboard
        if (IsCornerPosition(firstShot) && _opponentShots.Count >= 5)
        {
            // Check if subsequent shots follow checkerboard pattern
            if (IsCheckerboardPattern(_opponentShots.Take(5).ToList()))
            {
                _detectedStrategy = OpponentStrategy.CornerCheckerboard;
                _detectionComplete = true;
                return;
            }
        }

        // If we have 5+ shots and still can't determine, mark as Other
        if (_opponentShots.Count >= 5)
        {
            _detectedStrategy = OpponentStrategy.Other;
            _detectionComplete = true;
        }
    }

    private static bool IsCenterPosition(Coordinate pos)
    {
        // Center is (4,4) on 10x10 board
        // Allow slight variation (3-5, 3-5)
        return pos.X >= 3 && pos.X <= 5 && pos.Y >= 3 && pos.Y <= 5;
    }

    private static bool IsCornerPosition(Coordinate pos)
    {
        // Corners: (0,0), (0,9), (9,0), (9,9)
        // Allow edge positions too
        return (pos.X <= 1 || pos.X >= 8) && (pos.Y <= 1 || pos.Y >= 8);
    }

    private static bool IsCheckerboardPattern(List<Coordinate> shots)
    {
        if (shots.Count < 3)
        {
            return false;
        }

        // Count how many consecutive shots skip by 2 on same row/col
        int skipBy2Count = 0;

        for (int i = 1; i < shots.Count; i++)
        {
            var prev = shots[i - 1];
            var curr = shots[i];

            // Same row, different columns (should skip by 2 for checkerboard)
            if (prev.Y == curr.Y)
            {
                int colDiff = Math.Abs(curr.X - prev.X);
                if (colDiff == 2)
                {
                    skipBy2Count++;
                }
            }

            // Different row by 1, alternating column parity
            if (Math.Abs(curr.Y - prev.Y) == 1)
            {
                // Check if columns alternate parity (even/odd or odd/even)
                if ((prev.X % 2) != (curr.X % 2))
                {
                    skipBy2Count++;
                }
            }
        }

        // If at least 2 moves follow checkerboard pattern, it's likely checkerboard
        return skipBy2Count >= 2;
    }
}
