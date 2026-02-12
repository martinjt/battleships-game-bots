using BattleshipsBot.Common.Interfaces;

namespace SharpShooter;

/// <summary>
/// Factory for creating AdaptiveFiringStrategy instances
/// </summary>
public class AdaptiveFiringStrategyFactory : IFiringStrategyFactory
{
    private readonly OpponentDetector _sharedOpponentDetector;

    public AdaptiveFiringStrategyFactory(OpponentDetector sharedOpponentDetector)
    {
        _sharedOpponentDetector = sharedOpponentDetector;
    }

    public IFiringStrategy CreateStrategy()
    {
        return new AdaptiveFiringStrategy(_sharedOpponentDetector);
    }
}
