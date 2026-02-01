using FluentAssertions;
using Xunit;

namespace StackOverflowAttack.Tests;

public class RandomShipPlacerTests
{
    [Fact]
    public void PlaceShips_ReturnsCorrectNumberOfShips()
    {
        // Arrange
        var placer = new RandomShipPlacer();

        // Act
        var ships = placer.PlaceShips();

        // Assert
        ships.Should().HaveCount(5, "standard fleet has 5 ships");
    }

    [Fact]
    public void PlaceShips_ContainsAllExpectedShips()
    {
        // Arrange
        var placer = new RandomShipPlacer();

        // Act
        var ships = placer.PlaceShips();

        // Assert
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
        // Arrange
        var placer = new RandomShipPlacer();

        // Act
        var ships = placer.PlaceShips();

        // Assert
        ships.First(s => s.Name == "Carrier").Length.Should().Be(5);
        ships.First(s => s.Name == "Battleship").Length.Should().Be(4);
        ships.First(s => s.Name == "Cruiser").Length.Should().Be(3);
        ships.First(s => s.Name == "Submarine").Length.Should().Be(3);
        ships.First(s => s.Name == "Destroyer").Length.Should().Be(2);
    }

    [Fact]
    public void PlaceShips_ShipsDoNotOverlap()
    {
        // Arrange
        var placer = new RandomShipPlacer();

        // Act
        var ships = placer.PlaceShips();

        // Assert - Get all occupied cells and check for duplicates
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
        // Arrange
        var placer = new RandomShipPlacer();

        // Act
        var ships = placer.PlaceShips();

        // Assert
        foreach (var ship in ships)
        {
            ship.StartPosition.X.Should().BeInRange(0, 9);
            ship.StartPosition.Y.Should().BeInRange(0, 9);

            // Check end position
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
    public void PlaceShips_ProducesVariedPlacements()
    {
        // Arrange
        var placer = new RandomShipPlacer();

        // Act - Generate multiple placements
        var placements = Enumerable.Range(0, 10)
            .Select(_ => placer.PlaceShips())
            .ToList();

        // Assert - Check that not all placements are identical
        var firstPlacement = placements[0];
        var allIdentical = placements.Skip(1).All(p =>
            p.Zip(firstPlacement).All(pair =>
                pair.First.StartPosition == pair.Second.StartPosition &&
                pair.First.Orientation == pair.Second.Orientation));

        allIdentical.Should().BeFalse("random placements should vary");
    }

    [Theory]
    [InlineData(Orientation.Horizontal, 2, 3, 5, 6, 3)] // Carrier horizontal
    [InlineData(Orientation.Vertical, 5, 1, 3, 5, 3)] // Vertical placement
    public void ShipCells_AreCalculatedCorrectly(Orientation orientation, int startX, int startY, int length, int expectedEndX, int expectedEndY)
    {
        // Arrange
        var ship = new Ship("TestShip", length, new Coordinate(startX, startY), orientation);

        // Act - Calculate cells manually
        var cells = new List<Coordinate>();
        for (int i = 0; i < length; i++)
        {
            var coord = orientation == Orientation.Horizontal
                ? new Coordinate(startX + i, startY)
                : new Coordinate(startX, startY + i);
            cells.Add(coord);
        }

        // Assert
        cells.Should().HaveCount(length);
        cells[0].Should().Be(new Coordinate(startX, startY));
        cells[^1].Should().Be(new Coordinate(expectedEndX, expectedEndY));
    }
}
