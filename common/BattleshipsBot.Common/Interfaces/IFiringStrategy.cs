namespace BattleshipsBot.Common.Interfaces;

/// <summary>
/// Strategy for determining where to fire shots during a game
/// </summary>
public interface IFiringStrategy
{
    /// <summary>
    /// Get the next coordinate to fire at
    /// </summary>
    Coordinate GetNextShot();

    /// <summary>
    /// Record that a shot at the given coordinate was a hit
    /// </summary>
    void RecordHit(Coordinate coordinate);

    /// <summary>
    /// Record that a shot at the given coordinate was a miss
    /// </summary>
    void RecordMiss(Coordinate coordinate);

    /// <summary>
    /// Record that the last hit sunk a ship
    /// </summary>
    void RecordSunk();

    /// <summary>
    /// Reset the strategy to its initial state
    /// </summary>
    void Reset();
}

/// <summary>
/// Factory for creating game-specific firing strategy instances
/// </summary>
public interface IFiringStrategyFactory
{
    /// <summary>
    /// Create a new firing strategy instance for a game
    /// </summary>
    IFiringStrategy CreateStrategy();
}
