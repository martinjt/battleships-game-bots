using FluentAssertions;
using StackOverflowAttack.Strategies;
using Xunit;

namespace StackOverflowAttack.Tests;

public class LeftToRightFiringStrategyTests
{
    [Fact]
    public void GetNextShot_StartsAtOrigin()
    {
        // Arrange
        var strategy = new LeftToRightFiringStrategy();

        // Act
        var shot = strategy.GetNextShot();

        // Assert
        shot.Should().Be(new Coordinate(0, 0));
    }

    [Fact]
    public void GetNextShot_MovesLeftToRight()
    {
        // Arrange
        var strategy = new LeftToRightFiringStrategy();

        // Act
        var shots = Enumerable.Range(0, 5).Select(_ => strategy.GetNextShot()).ToList();

        // Assert
        shots.Should().Equal(
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0),
            new Coordinate(3, 0),
            new Coordinate(4, 0)
        );
    }

    [Fact]
    public void GetNextShot_WrapsToNextRow()
    {
        // Arrange
        var strategy = new LeftToRightFiringStrategy();

        // Act - Skip first 10 shots to get to row 1
        for (int i = 0; i < 10; i++)
        {
            strategy.GetNextShot();
        }
        var shot = strategy.GetNextShot();

        // Assert
        shot.Should().Be(new Coordinate(0, 1));
    }

    [Fact]
    public void GetNextShot_CoversEntireBoard()
    {
        // Arrange
        var strategy = new LeftToRightFiringStrategy();
        var expectedShots = new HashSet<Coordinate>();

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                expectedShots.Add(new Coordinate(x, y));
            }
        }

        // Act
        var shots = Enumerable.Range(0, 100).Select(_ => strategy.GetNextShot()).ToHashSet();

        // Assert
        shots.Should().BeEquivalentTo(expectedShots);
    }

    [Fact]
    public void GetNextShot_WrapsAroundAfterFullBoard()
    {
        // Arrange
        var strategy = new LeftToRightFiringStrategy();

        // Act - Get 101 shots (full board + 1)
        for (int i = 0; i < 100; i++)
        {
            strategy.GetNextShot();
        }
        var shot = strategy.GetNextShot();

        // Assert
        shot.Should().Be(new Coordinate(0, 0), "should wrap back to origin");
    }

    [Fact]
    public void Reset_ReturnsToOrigin()
    {
        // Arrange
        var strategy = new LeftToRightFiringStrategy();
        for (int i = 0; i < 25; i++)
        {
            strategy.GetNextShot();
        }

        // Act
        strategy.Reset();
        var shot = strategy.GetNextShot();

        // Assert
        shot.Should().Be(new Coordinate(0, 0));
    }
}

public class ProbabilityDensityFiringStrategyTests
{
    [Fact]
    public void RecordHit_IdempotentWhenCalledMultipleTimes()
    {
        // Arrange
        var strategy = new ProbabilityDensityFiringStrategy();
        var hitCoordinate = new Coordinate(5, 5);

        // Get initial shot to establish some state
        strategy.GetNextShot();

        // Act - Record the same hit multiple times (simulating replay bug)
        strategy.RecordHit(hitCoordinate);
        strategy.RecordHit(hitCoordinate);
        strategy.RecordHit(hitCoordinate);

        // Get next 10 shots
        var shots = Enumerable.Range(0, 10).Select(_ => strategy.GetNextShot()).ToList();

        // Assert - No shot should be repeated
        shots.Should().OnlyHaveUniqueItems("each shot should be unique even after replaying hits");

        // Assert - The hit coordinate should not be fired at again
        shots.Should().NotContain(hitCoordinate, "should not fire at a recorded hit");
    }

    [Fact]
    public void RecordMiss_AddsToFiredShots()
    {
        // Arrange
        var strategy = new ProbabilityDensityFiringStrategy();
        var missCoordinate = new Coordinate(3, 3);

        // Act - Record a miss
        strategy.RecordMiss(missCoordinate);

        // Get next 50 shots
        var shots = Enumerable.Range(0, 50).Select(_ => strategy.GetNextShot()).ToList();

        // Assert - The miss coordinate should not be fired at
        shots.Should().NotContain(missCoordinate, "should not fire at a recorded miss");
    }

    [Fact]
    public void ReplayingMultipleShotsDoesNotCauseDuplicates()
    {
        // Arrange
        var strategy = new ProbabilityDensityFiringStrategy();

        // Simulate a game where we fired 5 shots and got results
        var shotHistory = new List<(Coordinate, bool)>
        {
            (new Coordinate(4, 4), false), // Miss
            (new Coordinate(5, 5), true),  // Hit
            (new Coordinate(5, 6), true),  // Hit
            (new Coordinate(6, 5), false), // Miss
            (new Coordinate(5, 7), false)  // Miss
        };

        // Act - Replay the shot history multiple times (simulating the bug)
        for (int replay = 0; replay < 3; replay++)
        {
            foreach (var (coord, isHit) in shotHistory)
            {
                if (isHit)
                    strategy.RecordHit(coord);
                else
                    strategy.RecordMiss(coord);
            }
        }

        // Get next 20 shots
        var newShots = Enumerable.Range(0, 20).Select(_ => strategy.GetNextShot()).ToList();

        // Assert - No shot should be repeated
        newShots.Should().OnlyHaveUniqueItems("shots should be unique");

        // Assert - None of the replayed shots should be fired at again
        var firedCoordinates = shotHistory.Select(s => s.Item1);
        newShots.Should().NotContain(c => firedCoordinates.Contains(c),
            "should not fire at coordinates from shot history");
    }

    [Fact]
    public void GetNextShot_NeverReturnsNull()
    {
        // Arrange
        var strategy = new ProbabilityDensityFiringStrategy();

        // Act - Get 100 shots
        var shots = Enumerable.Range(0, 100).Select(_ => strategy.GetNextShot()).ToList();

        // Assert
        shots.Should().NotContainNulls();
        shots.Should().HaveCount(100);
    }

    [Fact]
    public void GetNextShot_CoversEntireBoard()
    {
        // Arrange
        var strategy = new ProbabilityDensityFiringStrategy();

        // Act - Get all 100 possible shots
        var shots = Enumerable.Range(0, 100).Select(_ => strategy.GetNextShot()).ToHashSet();

        // Assert - All 100 board positions should be covered
        shots.Should().HaveCount(100);

        // Verify all positions are within bounds
        shots.Should().OnlyContain(c => c.X >= 0 && c.X < 10 && c.Y >= 0 && c.Y < 10);
    }
}
