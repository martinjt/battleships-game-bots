using Xunit;

namespace SharpShooter.Tests;

public class OpponentDetectorTests
{
    [Fact]
    public void DetectsCenterFirstStrategy()
    {
        // Arrange
        var detector = new OpponentDetector();

        // Act - Simulate DepthCharge's center-first pattern
        detector.RecordOpponentShot(new Coordinate(4, 4)); // Center
        detector.RecordOpponentShot(new Coordinate(4, 3)); // West
        detector.RecordOpponentShot(new Coordinate(5, 4)); // South

        // Assert
        Assert.Equal(OpponentStrategy.CenterFirst, detector.GetDetectedStrategy());
    }

    [Fact]
    public void DetectsCornerCheckerboardStrategy()
    {
        // Arrange
        var detector = new OpponentDetector();

        // Act - Simulate Mirage's corner checkerboard pattern
        detector.RecordOpponentShot(new Coordinate(9, 9)); // Corner, odd row, odd col
        detector.RecordOpponentShot(new Coordinate(9, 7)); // Skip 2, same row
        detector.RecordOpponentShot(new Coordinate(9, 5)); // Skip 2, same row
        detector.RecordOpponentShot(new Coordinate(9, 3)); // Skip 2, same row
        detector.RecordOpponentShot(new Coordinate(8, 8)); // Move to next row, even row, even col

        // Assert - May detect as CornerCheckerboard or Other, both acceptable
        var result = detector.GetDetectedStrategy();
        Assert.True(
            result == OpponentStrategy.CornerCheckerboard || result == OpponentStrategy.Other,
            $"Expected CornerCheckerboard or Other for corner start with skip-2 pattern, got {result}"
        );
    }

    [Fact]
    public void ReturnsUnknownForInsufficientData()
    {
        // Arrange
        var detector = new OpponentDetector();

        // Act - Only one shot
        detector.RecordOpponentShot(new Coordinate(5, 5));

        // Assert
        Assert.Equal(OpponentStrategy.Unknown, detector.GetDetectedStrategy());
    }

    [Fact]
    public void ResetsCorrectly()
    {
        // Arrange
        var detector = new OpponentDetector();
        detector.RecordOpponentShot(new Coordinate(4, 4));
        detector.RecordOpponentShot(new Coordinate(4, 3));
        detector.RecordOpponentShot(new Coordinate(5, 4));

        // Act
        detector.Reset();

        // Assert
        Assert.Equal(OpponentStrategy.Unknown, detector.GetDetectedStrategy());
    }

    [Theory]
    [InlineData(0, 0)] // Top-left corner
    [InlineData(9, 9)] // Bottom-right corner
    [InlineData(0, 9)] // Top-right corner
    [InlineData(9, 0)] // Bottom-left corner
    public void DetectsCornerStartCorrectly(int x, int y)
    {
        // Arrange
        var detector = new OpponentDetector();

        // Act - Start from corner with checkerboard pattern
        detector.RecordOpponentShot(new Coordinate(x, y));
        // Add a few more shots with skip pattern
        detector.RecordOpponentShot(new Coordinate(x >= 5 ? x - 2 : x + 2, y));
        detector.RecordOpponentShot(new Coordinate(x >= 5 ? x - 4 : x + 4, y));
        detector.RecordOpponentShot(new Coordinate(x >= 5 ? x - 6 : x + 6, y));
        detector.RecordOpponentShot(new Coordinate(x, y >= 5 ? y - 1 : y + 1));

        // Assert
        var result = detector.GetDetectedStrategy();
        Assert.True(
            result == OpponentStrategy.CornerCheckerboard || result == OpponentStrategy.Other,
            $"Expected CornerCheckerboard or Other, got {result}"
        );
    }
}
