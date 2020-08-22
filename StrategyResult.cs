namespace SudokuSolver
{
    internal class StrategyResult
    {
        public StrategyType StrategyType { get; private set; }
       
        public bool Success { get; set; }

        public StrategyResult(StrategyType strategyType)
        {
            StrategyType = strategyType;
        }

        public static StrategyResult EmptyResult => new StrategyResult(StrategyType.None);
    }
}
