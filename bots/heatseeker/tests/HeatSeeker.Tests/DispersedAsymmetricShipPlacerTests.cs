using FluentAssertions;
using Xunit;

namespace HeatSeeker.Tests;

public class DispersedAsymmetricShipPlacerTests
{
    [Fact]
    public void PlaceShips_ReturnsCorrectNumberOfShips()
    {
        var placer = new DispersedAsymmetricShipPlacer();

        var ships = placer.PlaceShips();

        ships.Should().HaveCount(5, "standard fleet has 5 ships");
    }

    [Fact]
    public void PlaceShips_ContainsAllExpectedShips()
    {
        var placer = new DispersedAsymmetricShipPlacer();

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
        var placer = new DispersedAsymmetricShipPlacer();

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
        var placer = new DispersedAsymmetricShipPlacer();

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
        var placer = new DispersedAsymmetricShipPlacer();

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
    public void PlaceShips_ShipsAreDispersed()
    {
        var placer = new DispersedAsymmetricShipPlacer();

        // Run multiple times and check average dispersion
        double totalMinDistance = 0;
        int runs = 10;

        for (int run = 0; run < runs; run++)
        {
            var ships = placer.PlaceShips();
            var minDistance = CalculateMinDistanceBetweenShips(ships);
            totalMinDistance += minDistance;
        }

        var avgMinDistance = totalMinDistance / runs;
        avgMinDistance.Should().BeGreaterThanOrEqualTo(1, "dispersed placer should spread ships apart");
    }

    private static double CalculateMinDistanceBetweenShips(List<Ship> ships)
    {
        double minDistance = double.MaxValue;

        for (int i = 0; i < ships.Count; i++)
        {
            for (int j = i + 1; j < ships.Count; j++)
            {
                var cells1 = GetShipCells(ships[i]);
                var cells2 = GetShipCells(ships[j]);

                foreach (var c1 in cells1)
                {
                    foreach (var c2 in cells2)
                    {
                        var distance = Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y);
                        minDistance = Math.Min(minDistance, distance);
                    }
                }
            }
        }

        return minDistance;
    }

    private static List<Coordinate> GetShipCells(Ship ship)
    {
        var cells = new List<Coordinate>();
        for (int i = 0; i < ship.Length; i++)
        {
            var cell = ship.Orientation == Orientation.Horizontal
                ? new Coordinate(ship.StartPosition.X + i, ship.StartPosition.Y)
                : new Coordinate(ship.StartPosition.X, ship.StartPosition.Y + i);
            cells.Add(cell);
        }
        return cells;
    }

    [Fact]
    public void PlaceShips_ProducesVariedPlacements()
    {
        var placer = new DispersedAsymmetricShipPlacer();

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
        var placer = new DispersedAsymmetricShipPlacer();

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
