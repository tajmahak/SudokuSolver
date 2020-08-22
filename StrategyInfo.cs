namespace SudokuSolver
{
    internal class StrategyInfo
    {
        public StrategyType StrategyType { get; private set; }
        public StrategyMethod StrategyMethod { get; private set; }
        public StrategyArea StrategyArea { get; private set; }

        public StrategyInfo(StrategyType strategyType, StrategyMethod strategyMethod, StrategyArea strategyArea)
        {
            StrategyType = strategyType;
            StrategyMethod = strategyMethod;
            StrategyArea = strategyArea;
        }

        public override string ToString()
        {
            return StrategyType.ToString();
        }
    }
}
