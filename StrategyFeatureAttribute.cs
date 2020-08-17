using System;

namespace SudokuSolver
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class StrategyFeatureAttribute : Attribute
    {
        public StrategyType StrategyType { get; private set; }
        public StrategyArea StrategyArea { get; private set; }

        public StrategyFeatureAttribute(StrategyType strategyType, StrategyArea strategyArea)
        {
            StrategyType = strategyType;
            StrategyArea = strategyArea;
        }
    }
}
