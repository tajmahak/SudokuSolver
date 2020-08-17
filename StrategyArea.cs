using System;

namespace SudokuSolver
{
    [Flags]
    internal enum StrategyArea
    {
        Table = 1,
        Block = 2,
        Line = 4,
    }
}
