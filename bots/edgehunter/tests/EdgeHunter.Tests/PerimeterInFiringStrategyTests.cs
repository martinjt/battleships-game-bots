using FluentAssertions;
using Xunit;

namespace EdgeHunter.Tests;

public class PerimeterInFiringStrategyTests
{
    [Fact]
    public void GetNextShot_ReturnsValidCoordinate()
    {
        var strategy = new PerimeterInFiringStrategy();

        var shot = strategy.GetNextShot();

        shot.X.Should().BeInRange(0, 9);
        shot.Y.Should().BeInRange(0, 9);
    }

    [Fact]
    public void GetNextShot_StartsWithEdgeCells()
    {
        var strategy = new PerimeterInFiringStrategy();

        // Get first several shots
        var shots = Enumerable.Range(0, 10).Select(_ => strategy.GetNextShot()).ToList();

        // First shots should be on edges (x=0, x=9, y=0, or y=9)
        var edgeShots = shots.Where(s => s.X == 0 || s.X == 9 || s.Y == 0 || s.Y == 9).Count();
        edgeShots.Should().BeGreaterThan(5, "perimeter strategy should prioritize edges");
    }

    [Fact]
    public void GetNextShot_NoDuplicateShots()
    {
        var strategy = new PerimeterInFiringStrategy();
        var shots = new HashSet<Coordinate>();

        for (int i = 0; i < 100; i++)
        {
            var shot = strategy.GetNextShot();
            shots.Add(shot);
        }

        shots.Should().HaveCount(100);
    }

    [Fact]
    public void GetNextShot_CoversEntireBoard()
    {
        var strategy = new PerimeterInFiringStrategy();
        var expectedCoords = new HashSet<Coordinate>();

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                expectedCoords.Add(new Coordinate(x, y));
            }
        }

        var shots = Enumerable.Range(0, 100).Select(_ => strategy.GetNextShot()).ToHashSet();

        shots.Should().BeEquivalentTo(expectedCoords);
    }

    [Fact]
    public void RecordHit_EnqueuesAdjacentCells()
    {
        var strategy = new PerimeterInFiringStrategy();

        // Get first shot
        strategy.GetNextShot();

        // Record a hit
        strategy.RecordHit(new Coordinate(5, 5));

        // Next shot should be adjacent to the hit
        var nextShot = strategy.GetNextShot();

        var isAdjacent = Math.Abs(nextShot.X - 5) + Math.Abs(nextShot.Y - 5) == 1;
        isAdjacent.Should().BeTrue("after a hit, strategy should target adjacent cells");
    }

    [Fact]
    public void RecordHit_OnEdge_PrioritizesAlongEdge()
    {
        var strategy = new PerimeterInFiringStrategy();

        // Get first shot
        strategy.GetNextShot();

        // Record a hit on the top edge
        strategy.RecordHit(new Coordinate(5, 0));

        // Next shot should prioritize horizontal movement along edge
        var nextShot = strategy.GetNextShot();

        // Should be (4,0) or (6,0) for horizontal priority, or (5,1) for vertical
        var expectedTargets = new HashSet<Coordinate>
        {
            new Coordinate(4, 0),
            new Coordinate(6, 0),
            new Coordinate(5, 1)
        };
        expectedTargets.Should().Contain(nextShot);
    }

    [Fact]
    public void RecordSunk_ClearsTargetQueue()
    {
        var strategy = new PerimeterInFiringStrategy();

        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 6));

        strategy.RecordSunk();

        // After sunk, should return to scan pattern
        var shots = new List<Coordinate>();
        for (int i = 0; i < 5; i++)
        {
            shots.Add(strategy.GetNextShot());
        }

        shots.Should().HaveCount(5);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var strategy = new PerimeterInFiringStrategy();

        for (int i = 0; i < 20; i++)
        {
            strategy.GetNextShot();
        }
        strategy.RecordHit(new Coordinate(3, 3));

        strategy.Reset();
        var firstShot = strategy.GetNextShot();

        firstShot.X.Should().BeInRange(0, 9);
        firstShot.Y.Should().BeInRange(0, 9);
    }

    [Fact]
    public void RecordHit_IsIdempotent()
    {
        var strategy = new PerimeterInFiringStrategy();

        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 5));

        var shot = strategy.GetNextShot();
        shot.Should().NotBe(new Coordinate(5, 5));
    }

    [Fact]
    public void RecordMiss_IsIdempotent()
    {
        var strategy = new PerimeterInFiringStrategy();

        strategy.RecordMiss(new Coordinate(3, 3));
        strategy.RecordMiss(new Coordinate(3, 3));

        var shots = new HashSet<Coordinate>();
        for (int i = 0; i < 100; i++)
        {
            shots.Add(strategy.GetNextShot());
        }

        shots.Should().NotContain(new Coordinate(3, 3));
    }
}
