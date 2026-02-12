namespace BattleshipsBot.Common.Interfaces;

public enum Orientation
{
    Horizontal,
    Vertical
}

public record Coordinate(int X, int Y);

public record Ship(string Name, int Length, Coordinate StartPosition, Orientation Orientation);

public interface IShipPlacer
{
    List<Ship> PlaceShips();
}
