namespace StackOverflowAttack;

public record Coordinate(int X, int Y);

public interface IFiringStrategy
{
    Coordinate GetNextShot();
    void Reset();
}

public class LeftToRightFiringStrategy : IFiringStrategy
{
    private int _currentX = 0;
    private int _currentY = 0;
    private const int BoardSize = 10;

    public Coordinate GetNextShot()
    {
        var shot = new Coordinate(_currentX, _currentY);

        // Move left to right, top to bottom
        _currentX++;
        if (_currentX >= BoardSize)
        {
            _currentX = 0;
            _currentY++;
            if (_currentY >= BoardSize)
            {
                _currentY = 0; // Wrap around to start
            }
        }

        return shot;
    }

    public void Reset()
    {
        _currentX = 0;
        _currentY = 0;
    }
}
