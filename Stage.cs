using System.Collections.Generic;

namespace SudokuSolver
{
    internal class Stage
    {
        public Table Table { get; private set; }

        public Stage(Table table)
        {
            Table = table;
        }
    }
}
