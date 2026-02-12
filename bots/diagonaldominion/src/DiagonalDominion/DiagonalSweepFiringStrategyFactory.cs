using BattleshipsBot.Common.Interfaces;

namespace DiagonalDominion;

public class DiagonalSweepFiringStrategyFactory : IFiringStrategyFactory
{
    public IFiringStrategy CreateStrategy() => new DiagonalSweepFiringStrategy();
}
