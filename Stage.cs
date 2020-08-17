using System.Collections.Generic;

namespace SudokuSolver
{
    internal class Stage
    {
        public Table Table { get; private set; }

        public StrategyType StrategyType { get; private set; }

        public Stage(Table table, StrategyType strategyType)
        {
            Table = table;
            StrategyType = strategyType;
        }
    }
}
