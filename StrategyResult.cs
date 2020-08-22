namespace SudokuSolver
{
    internal class StrategyResult
    {
        public bool Success { get; set; }

        public StrategyType StrategyType { get; set; }

        public static StrategyResult EmptyResult => new StrategyResult();
    }
}
