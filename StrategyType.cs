namespace SudokuSolver
{
    internal enum StrategyType
    {
        CreateTable,
        InitializeProbableValues,

        SetValueToSolvedCell,

        HiddenSingles,
        NakedPairsTriples,
        HiddenPairsTriples,
        NakedQuards,
        HiddenQuards,
        PointingPairs,
        BoxLineReduction,

        XWing,
        SimpleColouring,
        YWing,
        Swordfish,
        XYZWing,
    }
}
