using BattleshipsBot.Common.Interfaces;

namespace EdgeHunter;

public class PerimeterInFiringStrategyFactory : IFiringStrategyFactory
{
    public IFiringStrategy CreateStrategy() => new PerimeterInFiringStrategy();
}
