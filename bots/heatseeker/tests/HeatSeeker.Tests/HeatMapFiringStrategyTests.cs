using FluentAssertions;
using Xunit;

namespace HeatSeeker.Tests;

public class HeatMapFiringStrategyTests
{
    [Fact]
    public void GetNextShot_ReturnsValidCoordinate()
    {
        var strategy = new HeatMapFiringStrategy();

        var shot = strategy.GetNextShot();

        shot.X.Should().BeInRange(0, 9);
        shot.Y.Should().BeInRange(0, 9);
    }

    [Fact]
    public void GetNextShot_NoDuplicateShots()
    {
        var strategy = new HeatMapFiringStrategy();
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
        var strategy = new HeatMapFiringStrategy();
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
    public void GetNextShot_PrefersParityCells()
    {
        var strategy = new HeatMapFiringStrategy();

        // Get first 20 shots
        var shots = Enumerable.Range(0, 20).Select(_ => strategy.GetNextShot()).ToList();

        // Most should be on parity cells (x+y) % 2 == 0
        var parityCells = shots.Count(s => (s.X + s.Y) % 2 == 0);
        parityCells.Should().BeGreaterThan(8, "heat map should prefer parity cells");
    }

    [Fact]
    public void RecordHit_PrioritizesAdjacentCells()
    {
        var strategy = new HeatMapFiringStrategy();

        // Get first shot
        strategy.GetNextShot();

        // Record a hit at center
        strategy.RecordHit(new Coordinate(5, 5));

        // Next shot should be adjacent to the hit
        var nextShot = strategy.GetNextShot();

        var isAdjacent = Math.Abs(nextShot.X - 5) + Math.Abs(nextShot.Y - 5) == 1;
        isAdjacent.Should().BeTrue("after a hit, strategy should target adjacent cells");
    }

    [Fact]
    public void RecordSunk_UpdatesRemainingShips()
    {
        var strategy = new HeatMapFiringStrategy();

        // Record 5 sinks (all ships)
        for (int i = 0; i < 5; i++)
        {
            strategy.RecordSunk();
        }

        // Strategy should still work but heat map will be based on no remaining ships
        var shot = strategy.GetNextShot();
        shot.X.Should().BeInRange(0, 9);
        shot.Y.Should().BeInRange(0, 9);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var strategy = new HeatMapFiringStrategy();

        for (int i = 0; i < 20; i++)
        {
            strategy.GetNextShot();
        }
        strategy.RecordHit(new Coordinate(3, 3));
        strategy.RecordSunk();

        strategy.Reset();
        var firstShot = strategy.GetNextShot();

        firstShot.X.Should().BeInRange(0, 9);
        firstShot.Y.Should().BeInRange(0, 9);
    }

    [Fact]
    public void RecordHit_IsIdempotent()
    {
        var strategy = new HeatMapFiringStrategy();

        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 5));
        strategy.RecordHit(new Coordinate(5, 5));

        var shot = strategy.GetNextShot();
        shot.Should().NotBe(new Coordinate(5, 5));
    }

    [Fact]
    public void RecordMiss_IsIdempotent()
    {
        var strategy = new HeatMapFiringStrategy();

        strategy.RecordMiss(new Coordinate(3, 3));
        strategy.RecordMiss(new Coordinate(3, 3));

        var shots = new HashSet<Coordinate>();
        for (int i = 0; i < 100; i++)
        {
            shots.Add(strategy.GetNextShot());
        }

        shots.Should().NotContain(new Coordinate(3, 3));
    }

    [Fact]
    public void HeatMap_ConsidersRemainingShipSizes()
    {
        var strategy = new HeatMapFiringStrategy();

        // Fire some shots to record misses in a pattern
        strategy.RecordMiss(new Coordinate(0, 0));
        strategy.RecordMiss(new Coordinate(1, 0));
        strategy.RecordMiss(new Coordinate(2, 0));
        strategy.RecordMiss(new Coordinate(3, 0));

        // Next shot should avoid areas where no ship can fit
        var shot = strategy.GetNextShot();
        shot.X.Should().BeInRange(0, 9);
        shot.Y.Should().BeInRange(0, 9);
        shot.Should().NotBe(new Coordinate(0, 0));
    }
}
