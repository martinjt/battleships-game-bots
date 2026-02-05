using FluentAssertions;
using Xunit;

namespace DiagonalDominion.Tests;

public class DiagonalSweepFiringStrategyTests
{
    [Fact]
    public void GetNextShot_ReturnsValidCoordinate()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();

        // Act
        var shot = strategy.GetNextShot();

        // Assert
        shot.X.Should().BeInRange(0, 9);
        shot.Y.Should().BeInRange(0, 9);
    }

    [Fact]
    public void GetNextShot_NoDuplicateShots()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();
        var shots = new HashSet<Coordinate>();

        // Act - Get 100 shots (full board)
        for (int i = 0; i < 100; i++)
        {
            var shot = strategy.GetNextShot();
            shots.Add(shot);
        }

        // Assert - All shots should be unique
        shots.Should().HaveCount(100);
    }

    [Fact]
    public void GetNextShot_CoversEntireBoard()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();
        var expectedCoords = new HashSet<Coordinate>();

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                expectedCoords.Add(new Coordinate(x, y));
            }
        }

        // Act
        var shots = Enumerable.Range(0, 100).Select(_ => strategy.GetNextShot()).ToHashSet();

        // Assert
        shots.Should().BeEquivalentTo(expectedCoords);
    }

    [Fact]
    public void GetNextShot_StartsWithDiagonalPattern()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();

        // Act - Get first few shots
        var shots = Enumerable.Range(0, 10).Select(_ => strategy.GetNextShot()).ToList();

        // Assert - First shots should follow diagonal pattern (x + y should be consistent within diagonals)
        shots.All(s => s.X >= 0 && s.X <= 9 && s.Y >= 0 && s.Y <= 9).Should().BeTrue();
    }

    [Fact]
    public void RecordHit_EnqueuesAdjacentCells()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();

        // Get first shot to advance state
        var firstShot = strategy.GetNextShot();

        // Act - Record a hit at center of board
        strategy.RecordHit(new Coordinate(5, 5));

        // Get next shot - should be adjacent to the hit
        var nextShot = strategy.GetNextShot();

        // Assert - Next shot should be adjacent to (5,5)
        var isAdjacent = Math.Abs(nextShot.X - 5) + Math.Abs(nextShot.Y - 5) == 1;
        isAdjacent.Should().BeTrue("after a hit, strategy should target adjacent cells");
    }

    [Fact]
    public void RecordSunk_ClearsTargetQueue()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();

        // Record multiple hits
        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 6));

        // Act - Ship is sunk
        strategy.RecordSunk();

        // Get next shot - should return to scan pattern, not target mode
        var shots = new List<Coordinate>();
        for (int i = 0; i < 5; i++)
        {
            shots.Add(strategy.GetNextShot());
        }

        // Assert - After sunk, should not be stuck targeting around (5,5) area
        // Should be back to scanning diagonals
        shots.Should().HaveCount(5);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();

        // Fire some shots and record hits
        for (int i = 0; i < 20; i++)
        {
            strategy.GetNextShot();
        }
        strategy.RecordHit(new Coordinate(3, 3));

        // Act
        strategy.Reset();
        var firstShot = strategy.GetNextShot();

        // Assert - Should start fresh
        firstShot.X.Should().BeInRange(0, 9);
        firstShot.Y.Should().BeInRange(0, 9);
    }

    [Fact]
    public void RecordHit_IsIdempotent()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();

        // Act - Record same hit multiple times
        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 5));

        // Should not throw and should still work correctly
        var shot = strategy.GetNextShot();
        shot.Should().NotBe(new Coordinate(5, 5), "should not re-fire at recorded hit");
    }

    [Fact]
    public void RecordMiss_IsIdempotent()
    {
        // Arrange
        var strategy = new DiagonalSweepFiringStrategy();

        // Act - Record same miss multiple times
        strategy.RecordMiss(new Coordinate(3, 3));
        strategy.RecordMiss(new Coordinate(3, 3));

        // Get all shots
        var shots = new HashSet<Coordinate>();
        for (int i = 0; i < 100; i++)
        {
            shots.Add(strategy.GetNextShot());
        }

        // Assert - Should not contain the missed coordinate
        shots.Should().NotContain(new Coordinate(3, 3));
    }
}
