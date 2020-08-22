namespace SudokuSolver
{
    internal class Stage
    {
        public Table Table { get; private set; }

        public StrategyResult Result { get; private set; }

        public Stage(Table table, StrategyResult result)
        {
            Table = table;
            Result = result;
        }
    }
}
