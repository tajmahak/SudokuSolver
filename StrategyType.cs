namespace SudokuSolver
{
    internal enum StrategyType
    {
        None,
        CreateTable,
        InitializeProbableValues,

        SetValueToSolvedCell,

        HiddenSingles,
        //NakedPairsTriples,
        //HiddenPairsTriples,
        //NakedQuards,
        //HiddenQuards,
        //PointingPairs,
        //BoxLineReduction,

        //XWing,
    }
}
