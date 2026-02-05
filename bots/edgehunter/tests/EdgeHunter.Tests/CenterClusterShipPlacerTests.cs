using FluentAssertions;
using Xunit;

namespace EdgeHunter.Tests;

public class CenterClusterShipPlacerTests
{
    [Fact]
    public void PlaceShips_ReturnsCorrectNumberOfShips()
    {
        var placer = new CenterClusterShipPlacer();

        var ships = placer.PlaceShips();

        ships.Should().HaveCount(5, "standard fleet has 5 ships");
    }

    [Fact]
    public void PlaceShips_ContainsAllExpectedShips()
    {
        var placer = new CenterClusterShipPlacer();

        var ships = placer.PlaceShips();

        ships.Select(s => s.Name).Should().BeEquivalentTo(new[]
        {
            "Carrier",
            "Battleship",
            "Cruiser",
            "Submarine",
            "Destroyer"
        });
    }

    [Fact]
    public void PlaceShips_ShipsHaveCorrectLengths()
    {
        var placer = new CenterClusterShipPlacer();

        var ships = placer.PlaceShips();

        ships.First(s => s.Name == "Carrier").Length.Should().Be(5);
        ships.First(s => s.Name == "Battleship").Length.Should().Be(4);
        ships.First(s => s.Name == "Cruiser").Length.Should().Be(3);
        ships.First(s => s.Name == "Submarine").Length.Should().Be(3);
        ships.First(s => s.Name == "Destroyer").Length.Should().Be(2);
    }

    [Fact]
    public void PlaceShips_ShipsDoNotOverlap()
    {
        var placer = new CenterClusterShipPlacer();

        var ships = placer.PlaceShips();

        var occupiedCells = new List<Coordinate>();
        foreach (var ship in ships)
        {
            for (int i = 0; i < ship.Length; i++)
            {
                var coord = ship.Orientation == Orientation.Horizontal
                    ? new Coordinate(ship.StartPosition.X + i, ship.StartPosition.Y)
                    : new Coordinate(ship.StartPosition.X, ship.StartPosition.Y + i);
                occupiedCells.Add(coord);
            }
        }

        occupiedCells.Should().OnlyHaveUniqueItems("ships should not overlap");
    }

    [Fact]
    public void PlaceShips_ShipsStayWithinBoardBounds()
    {
        var placer = new CenterClusterShipPlacer();

        var ships = placer.PlaceShips();

        foreach (var ship in ships)
        {
            ship.StartPosition.X.Should().BeInRange(0, 9);
            ship.StartPosition.Y.Should().BeInRange(0, 9);

            if (ship.Orientation == Orientation.Horizontal)
            {
                (ship.StartPosition.X + ship.Length - 1).Should().BeInRange(0, 9);
                ship.StartPosition.Y.Should().BeInRange(0, 9);
            }
            else
            {
                ship.StartPosition.X.Should().BeInRange(0, 9);
                (ship.StartPosition.Y + ship.Length - 1).Should().BeInRange(0, 9);
            }
        }
    }

    [Fact]
    public void PlaceShips_PrefersCenterRegion()
    {
        var placer = new CenterClusterShipPlacer();

        // Run multiple times to check statistical tendency
        int centerPlacements = 0;
        int totalRuns = 20;

        for (int run = 0; run < totalRuns; run++)
        {
            var ships = placer.PlaceShips();

            // Count ships that have at least one cell in center region (2,2) to (7,7)
            foreach (var ship in ships)
            {
                for (int i = 0; i < ship.Length; i++)
                {
                    var coord = ship.Orientation == Orientation.Horizontal
                        ? new Coordinate(ship.StartPosition.X + i, ship.StartPosition.Y)
                        : new Coordinate(ship.StartPosition.X, ship.StartPosition.Y + i);

                    if (coord.X >= 2 && coord.X <= 7 && coord.Y >= 2 && coord.Y <= 7)
                    {
                        centerPlacements++;
                        break;
                    }
                }
            }
        }

        // Most ships should touch center region
        centerPlacements.Should().BeGreaterThan(totalRuns * 3, "center cluster placer should prefer center");
    }

    [Fact]
    public void PlaceShips_ProducesVariedPlacements()
    {
        var placer = new CenterClusterShipPlacer();

        var placements = Enumerable.Range(0, 10)
            .Select(_ => placer.PlaceShips())
            .ToList();

        var firstPlacement = placements[0];
        var allIdentical = placements.Skip(1).All(p =>
            p.Zip(firstPlacement).All(pair =>
                pair.First.StartPosition == pair.Second.StartPosition &&
                pair.First.Orientation == pair.Second.Orientation));

        allIdentical.Should().BeFalse("placements should vary");
    }

    [Fact]
    public void PlaceShips_MultipleCalls_AllValid()
    {
        var placer = new CenterClusterShipPlacer();

        for (int run = 0; run < 50; run++)
        {
            var ships = placer.PlaceShips();

            ships.Should().HaveCount(5);

            var occupiedCells = new HashSet<Coordinate>();
            foreach (var ship in ships)
            {
                for (int i = 0; i < ship.Length; i++)
                {
                    var coord = ship.Orientation == Orientation.Horizontal
                        ? new Coordinate(ship.StartPosition.X + i, ship.StartPosition.Y)
                        : new Coordinate(ship.StartPosition.X, ship.StartPosition.Y + i);

                    coord.X.Should().BeInRange(0, 9);
                    coord.Y.Should().BeInRange(0, 9);
                    occupiedCells.Add(coord).Should().BeTrue($"ship {ship.Name} overlaps on run {run}");
                }
            }
        }
    }
}
