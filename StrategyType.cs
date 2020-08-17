namespace SudokuSolver
{
    internal enum StrategyType
    {
        CreateTable,
        InitializeProbableValues,

        SetValueToSolvedCell,
        HiddenSingles,
        PointingPairs,
        NakedHiddenValueSet,
        BoxLineReduction,
    }
}
