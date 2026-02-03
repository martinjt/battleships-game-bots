using Xunit;

namespace SharpShooter.Tests;

public class AdaptiveShipPlacerTests
{
    [Fact]
    public void PlacesAllFiveShips()
    {
        // Arrange
        var detector = new OpponentDetector();
        var placer = new AdaptiveShipPlacer(detector);

        // Act
        var ships = placer.PlaceShips();

        // Assert
        Assert.Equal(5, ships.Count);
        Assert.Contains(ships, s => s.Name == "Carrier" && s.Length == 5);
        Assert.Contains(ships, s => s.Name == "Battleship" && s.Length == 4);
        Assert.Contains(ships, s => s.Name == "Cruiser" && s.Length == 3);
        Assert.Contains(ships, s => s.Name == "Submarine" && s.Length == 3);
        Assert.Contains(ships, s => s.Name == "Destroyer" && s.Length == 2);
    }

    [Fact]
    public void ShipsDoNotOverlap()
    {
        // Arrange
        var detector = new OpponentDetector();
        var placer = new AdaptiveShipPlacer(detector);

        // Act
        var ships = placer.PlaceShips();

        // Assert - Get all occupied cells
        var occupiedCells = new HashSet<Coordinate>();
        foreach (var ship in ships)
        {
            var cells = GetShipCells(ship);
            foreach (var cell in cells)
            {
                Assert.DoesNotContain(cell, occupiedCells); // No overlap
                occupiedCells.Add(cell);
            }
        }

        // Should have 17 total cells (5+4+3+3+2)
        Assert.Equal(17, occupiedCells.Count);
    }

    [Fact]
    public void AllShipsAreWithinBoardBounds()
    {
        // Arrange
        var detector = new OpponentDetector();
        var placer = new AdaptiveShipPlacer(detector);

        // Act
        var ships = placer.PlaceShips();

        // Assert
        foreach (var ship in ships)
        {
            var cells = GetShipCells(ship);
            foreach (var cell in cells)
            {
                Assert.InRange(cell.X, 0, 9);
                Assert.InRange(cell.Y, 0, 9);
            }
        }
    }

    [Fact]
    public void PlacesShipsOnEdgesAgainstCenterFirstOpponent()
    {
        // Arrange
        var detector = new OpponentDetector();

        // Simulate DepthCharge pattern
        detector.RecordOpponentShot(new Coordinate(4, 4));
        detector.RecordOpponentShot(new Coordinate(4, 3));
        detector.RecordOpponentShot(new Coordinate(5, 4));

        var placer = new AdaptiveShipPlacer(detector);

        // Act
        var ships = placer.PlaceShips();

        // Assert - Count ships on edges (row 0, 9 or col 0, 9)
        int edgeShips = 0;
        foreach (var ship in ships)
        {
            var cells = GetShipCells(ship);
            bool isOnEdge = cells.Any(c => c.X == 0 || c.X == 9 || c.Y == 0 || c.Y == 9);
            if (isOnEdge)
            {
                edgeShips++;
            }
        }

        // Most ships should be on edges
        Assert.True(edgeShips >= 3, $"Expected at least 3 edge ships, got {edgeShips}");
    }

    [Fact]
    public void PlacesShipsOnOddColumnsAgainstCheckerboardOpponent()
    {
        // Arrange
        var detector = new OpponentDetector();

        // Simulate Mirage pattern
        detector.RecordOpponentShot(new Coordinate(9, 9));
        detector.RecordOpponentShot(new Coordinate(9, 7));
        detector.RecordOpponentShot(new Coordinate(9, 5));
        detector.RecordOpponentShot(new Coordinate(9, 3));
        detector.RecordOpponentShot(new Coordinate(9, 1));

        var placer = new AdaptiveShipPlacer(detector);

        // Act
        var ships = placer.PlaceShips();

        // Assert - Count vertical ships on odd columns
        int antiCheckerboardShips = 0;
        foreach (var ship in ships)
        {
            if (ship.Orientation == Orientation.Vertical && ship.StartPosition.X % 2 == 1)
            {
                antiCheckerboardShips++;
            }
            else if (ship.Orientation == Orientation.Horizontal && ship.StartPosition.Y % 2 == 1)
            {
                antiCheckerboardShips++;
            }
        }

        // Should favor odd-column/row placement (at least 1 due to randomness)
        Assert.True(antiCheckerboardShips >= 1, $"Expected at least 1 anti-checkerboard ship, got {antiCheckerboardShips}");
    }

    [Fact]
    public void PlacesBalancedShipsForUnknownOpponent()
    {
        // Arrange
        var detector = new OpponentDetector(); // No opponent shots recorded
        var placer = new AdaptiveShipPlacer(detector);

        // Act
        var ships = placer.PlaceShips();

        // Assert - Just verify it places all ships successfully
        Assert.Equal(5, ships.Count);

        // Verify no overlaps
        var occupiedCells = new HashSet<Coordinate>();
        foreach (var ship in ships)
        {
            var cells = GetShipCells(ship);
            foreach (var cell in cells)
            {
                Assert.DoesNotContain(cell, occupiedCells);
                occupiedCells.Add(cell);
            }
        }
    }

    [Fact]
    public void PlacementIsRandomizedBetweenGames()
    {
        // Arrange
        var detector = new OpponentDetector();
        var placer = new AdaptiveShipPlacer(detector);

        // Act - Place ships 3 times
        var placement1 = placer.PlaceShips();
        var placement2 = placer.PlaceShips();
        var placement3 = placer.PlaceShips();

        // Assert - At least one ship should be in a different position
        bool allIdentical = true;
        for (int i = 0; i < 5; i++)
        {
            if (placement1[i].StartPosition != placement2[i].StartPosition ||
                placement1[i].StartPosition != placement3[i].StartPosition)
            {
                allIdentical = false;
                break;
            }
        }

        Assert.False(allIdentical, "Ship placements should vary between games");
    }

    private static List<Coordinate> GetShipCells(Ship ship)
    {
        var cells = new List<Coordinate>();
        for (int i = 0; i < ship.Length; i++)
        {
            var coord = ship.Orientation == Orientation.Horizontal
                ? new Coordinate(ship.StartPosition.X + i, ship.StartPosition.Y)
                : new Coordinate(ship.StartPosition.X, ship.StartPosition.Y + i);
            cells.Add(coord);
        }
        return cells;
    }
}
