using BattleshipsBot.Common.Interfaces;

namespace HeatSeeker;

public class HeatMapFiringStrategyFactory : IFiringStrategyFactory
{
    public IFiringStrategy CreateStrategy() => new HeatMapFiringStrategy();
}
