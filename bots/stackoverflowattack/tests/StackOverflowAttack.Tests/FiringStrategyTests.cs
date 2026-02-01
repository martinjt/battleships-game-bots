using FluentAssertions;
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
